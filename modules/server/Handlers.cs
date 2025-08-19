using System.Text.Json;
using project_nuclear_weapons_management_system.modules.database;
using BCryptNet = BCrypt.Net.BCrypt;

namespace project_nuclear_weapons_management_system.modules.server
{
    // Factory Method target
    public interface IRequestHandler { string Handle(string body); }

    public static class HandlerFactory
    {
        // Factory Method: chọn handler theo path
        public static IRequestHandler Create(string path, IUserRepository repo) => path switch
        {
            "/api/auth/login" => new LoginHandler(repo),
            _ => new NotFoundHandler()
        };
    }

    public sealed class NotFoundHandler : IRequestHandler
    {
        public string Handle(string _) => HttpHelper.Json(404, new { error = "Not found" });
    }

    public sealed class LoginHandler : IRequestHandler
    {
        private readonly IUserRepository _repo;
        public LoginHandler(IUserRepository repo) { _repo = repo; }

        private sealed record LoginRequest(string Username, string Password);

        public string Handle(string body)
        {
            try
            {
                var req = JsonSerializer.Deserialize<LoginRequest>(body,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (req is null || string.IsNullOrWhiteSpace(req.Username) || string.IsNullOrWhiteSpace(req.Password))
                    return HttpHelper.Json(400, new { error = "Invalid payload" });

                var user = _repo.FindByUsernameAsync(req.Username).GetAwaiter().GetResult();
                if (user is null || !BCryptNet.Verify(req.Password, user.PasswordHash))
                    return HttpHelper.Json(401, new { error = "Invalid username or password" });

                var token = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
                return HttpHelper.Json(200, new { token, user = new { user.Id, user.Username, user.Role } });
            }
            catch (Exception ex)
            {
                Logger.Log($"[ERROR] LoginHandler: {ex.Message}");
                return HttpHelper.Json(500, new { error = "Server error" });
            }
        }
    }
}
