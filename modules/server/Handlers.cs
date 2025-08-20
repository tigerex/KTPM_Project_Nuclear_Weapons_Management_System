using System.Text.Json;
using project_nuclear_weapons_management_system.modules.database;
using BCryptNet = BCrypt.Net.BCrypt;

namespace project_nuclear_weapons_management_system.modules.server
{
    // Factory Method target
    public interface IRequestHandler
    {
        byte[] Handle(string body);  // now returns byte[]
    }

    public static class HandlerFactory
    {
        // Factory Method: chọn handler theo path
        public static IRequestHandler Create(string path, IUserRepository repo) => path switch
        {
            // API cho user
            "/api/auth/login" => new LoginHandler(repo),
            "/api/auth/logout" => new LogoutHandler(repo),

            // API cho kho vũ khí
            "/api/storages/" => new StoreageHandler(), // GET một kho vũ khí (với ID hay gì đó)
            "/api/storages/all" => new AllStorageHandler(), // GET all kho vũ khí
            "/api/storages/add" => new AddStorageHandler(), // POST kho vũ khí
            "/api/storages/delete" => new DeleteStorageHandler(), // DELETE một kho vũ khí
            "/api/storages/update" => new UpdateStorageHandler(), // PUT kho vũ khí

            // API cho vũ khí
            "/api/weapons/all" => new AllWeaponHandler(), // GET all vũ khí
            "/api/weapons/" => new WeaponHandler(), // // GET một vũ khí (với ID hay gì đó)
            "/api/weapons/add" => new AddWeaponHandler(), // POST vũ khí
            "/api/weapons/delete" => new DeleteWeaponHandler(), // DELETE một vũ khí
            "/api/weapons/update" => new UpdateWeaponHandler(), // PUT vũ khí
            
            _ => new NotFoundHandler() //Default if not found
        };
    }

    //Trong trường hợp endpoint not found
    public sealed class NotFoundHandler : IRequestHandler
    {
        public byte[] Handle(string _) =>
            HttpHelper.Json(404, new { error = "Endpoint Not found" });
    }

    //Endpoint login (tui tui chưa đụng vô)
    public sealed class LoginHandler : IRequestHandler
    {
        private readonly IUserRepository _repo;
        public LoginHandler(IUserRepository repo) { _repo = repo; }

        private sealed record LoginRequest(string Username, string Password);

        public byte[] Handle(string body)
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

    //Endpoint logout
    public sealed class LogoutHandler : IRequestHandler
    {
        public byte[] Handle(string body)
        {
            try
            {
                var storages = Database.GetAllStorages();
                return HttpHelper.Json(200, storages);
            }
            catch (Exception ex)
            {
                Logger.Log($"[ERROR] AllStorageHandler: {ex.Message}");
                return HttpHelper.Json(500, new { error = "Server error" });
            }
        }
    }

    //Handler GET tất cả kho vũ khí
    public sealed class AllStorageHandler : IRequestHandler
    {
        public byte[] Handle(string body)
        {
            try
            {
                var storages = Database.GetAllStorages();
                return HttpHelper.Json(200, storages);
            }
            catch (Exception ex)
            {
                Logger.Log($"[ERROR] AllStorageHandler: {ex.Message}");
                return HttpHelper.Json(500, new { error = "Server error" });
            }
        }
    }
    
    //Hanlder cho POST kho vũ khí
    public sealed class AddStorageHandler : IRequestHandler
    {
        public byte[] Handle(string body)
        {
            try
            {
                // parse body JSON: { "locationName": "...", "latitude": 10.7, "longitude": 106.6 }
                var doc = JsonDocument.Parse(body);
                var root = doc.RootElement;

                string locationName = root.GetProperty("location_name").GetString()!;
                decimal latitude = root.GetProperty("latitude").GetDecimal();
                decimal longitude = root.GetProperty("longitude").GetDecimal();

                int newId = Database.AddStorage(locationName, latitude, longitude);

                return HttpHelper.Json(201, new { id = newId, message = "Storage added successfully" });
            }
            catch (Exception ex)
            {
                Logger.Log($"[ERROR] AddStorageHandler: {ex.Message}");
                return HttpHelper.Json(500, new { error = "Server error" });
            }
        }
    }
}
