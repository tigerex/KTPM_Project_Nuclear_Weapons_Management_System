using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text.Json;
using project_nuclear_weapons_management_system.modules.database;
using BCryptNet = BCrypt.Net.BCrypt;

namespace project_nuclear_weapons_management_system.modules.server
{
    
    // ========================
    // AuthService (Singleton)
    // ========================
    internal sealed record Session(int UserId, string Username, string Role, DateTime ExpiresAt);

    internal sealed class AuthService
    {
        private static readonly Lazy<AuthService> _i = new(() => new AuthService());
        public static AuthService Instance => _i.Value;

        private readonly ConcurrentDictionary<string, Session> _sessions = new();

        private AuthService() { }

        public string Issue(int userId, string username, string role, TimeSpan? ttl = null)
        {
            var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
            _sessions[token] = new Session(userId, username, role, DateTime.UtcNow + (ttl ?? TimeSpan.FromHours(12)));
            return token;
        }

        public Session? Validate(string? authorizationHeader)
        {
            if (string.IsNullOrWhiteSpace(authorizationHeader)) return null;
            if (!authorizationHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)) return null;
            var token = authorizationHeader.Substring(7).Trim();
            return _sessions.TryGetValue(token, out var s) && s.ExpiresAt > DateTime.UtcNow ? s : null;
        }

        public void Revoke(string? authorizationHeader)
        {
            if (string.IsNullOrWhiteSpace(authorizationHeader)) return;
            if (!authorizationHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)) return;
            var token = authorizationHeader.Substring(7).Trim();
            _sessions.TryRemove(token, out _); // idempotent
        }
        
        //  public Session? ValidateToken(string token)
        // {
        //     if (string.IsNullOrWhiteSpace(token)) return null;
        //     return _sessions.TryGetValue(token, out var s) && s.ExpiresAt > DateTime.UtcNow ? s : null;
        // }

        // public void Revoke(string token)
        // {
        //     if (string.IsNullOrWhiteSpace(token)) return;
        //     _sessions.TryRemove(token, out _); // idempotent
        // }
    }

    // ===================================
    // IRequestHandler: trả về byte[]
    // ===================================
    public interface IRequestHandler
    {
        // byte[] Handle(string body); // GET có thể bỏ qua body

        // NHỚ: thêm headers ở đây để handler nào cần (logout, protected) có thể đọc Authorization
        byte[] Handle(string body, Dictionary<string,string> headers);
    }

    // ===================================
    // Factory Method: (method,path) -> handler
    // ===================================
    public static class HandlerFactory
    {
        // Factory Method: chọn handler theo path
        /// <summary>
        /// Chọn handler theo method/path.
        /// headers được truyền cho các handler cần kiểm tra token.
        /// </summary>
        public static IRequestHandler Create(string path, IUserRepository repo) => path switch
        {
            // API cho user
            "/api/auth/login" => new LoginHandler(repo),
            "/api/auth/logout" => new LogoutHandler(),

            // API cho kho vũ khí
            // "/api/storages/" => new StoreageHandler(), // GET một kho vũ khí (với ID hay gì đó)
            "/api/storages/all" => new AllStorageHandler(), // GET all kho vũ khí
            "/api/storages/add" => new AddStorageHandler(), // POST kho vũ khí
                                                            // "/api/storages/delete" => new DeleteStorageHandler(), // DELETE một kho vũ khí
                                                            // "/api/storages/update" => new UpdateStorageHandler(), // PUT kho vũ khí

            // API cho vũ khí
            // "/api/weapons/all" => new AllWeaponHandler(), // GET all vũ khí
            // "/api/weapons/" => new WeaponHandler(), // // GET một vũ khí (với ID hay gì đó)
            // "/api/weapons/add" => new AddWeaponHandler(), // POST vũ khí
            // "/api/weapons/delete" => new DeleteWeaponHandler(), // DELETE một vũ khí
            // "/api/weapons/update" => new UpdateWeaponHandler(), // PUT vũ khí

            _ => new NotFoundHandler() //Default if not found
        };
    }


    // ========================
    // Concrete handlers
    // ========================
    //Trong trường hợp endpoint not found
    public sealed class NotFoundHandler : IRequestHandler
    {
        public byte[] Handle(string _, Dictionary<string,string> headers) =>
            HttpHelper.Json(404, new { error = "Endpoint Not found" });
    }

    //Endpoint login (tui tui chưa đụng vô)
    public sealed class LoginHandler(IUserRepository repo) : IRequestHandler
    {
        private readonly IUserRepository _repo = repo;

        private sealed record LoginRequest(string Username, string Password);

        public byte[] Handle(string body, Dictionary<string,string> headers)
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

                // Tạo token và trả về bằng cách gọi AuthService ở đoạn trên nè
                var token = AuthService.Instance.Issue(user.Id, user.Username, user.Role);
                return HttpHelper.Json(200,
                    new { token, user = new { user.Id, user.Username, user.Role } },
                    new() {
                        // HttpOnly để JS không đọc được cookie (an toàn hơn); SameSite=Lax ok cho điều hướng nội bộ
                        ["Set-Cookie"] = $"auth={token}; Path=/; HttpOnly; SameSite=Lax"
                });
            }
            catch (Exception ex)
            {
                Logger.Log($"[ERROR] LoginHandler: {ex}");
                return HttpHelper.Json(500, new { error = "Server error" });
            }
        }
    }

    //Endpoint logout
    // ===== Logout (header-based) =====
    public sealed class LogoutHandler : IRequestHandler
    {
        public byte[] Handle(string body, Dictionary<string,string> headers)
        {
            try
            {
                // Lấy Authorization header (Bearer <token>) từ headers (đã parse ở HTTP parser)
                headers.TryGetValue("Authorization", out var authHeader);

                // Thu hồi token. Thiết kế idempotent: luôn 200 OK dù token có/không.
                AuthService.Instance.Revoke(authHeader);
                Console.WriteLine($"[INFO] User logged out: {authHeader}");
                return HttpHelper.Json(200, new { ok = true, message = "Logged out" });
            }
            catch (Exception ex)
            {
                Logger.Log($"[ERROR] LogoutHandler: {ex.Message}");
                return HttpHelper.Json(500, new { error = "Server error" });
            }
        }
    }

    //Handler GET tất cả kho vũ khí
    public sealed class AllStorageHandler : IRequestHandler
    {
        public byte[] Handle(string body, Dictionary<string,string> headers)
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
        public byte[] Handle(string body, Dictionary<string,string> headers)
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
