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

        public static bool RequireRole(Dictionary<string,string> headers, params string[] roles)
        {
            headers.TryGetValue("Authorization", out var authHeader);
            var session = AuthService.Instance.Validate(authHeader);
            if (session == null) return false;

            return roles.Contains(session.Role, StringComparer.OrdinalIgnoreCase);
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
            "/api/auth/me"     => new MeHandler(repo),

            // API cho kho vũ khí
            "/api/storages/" => new StorageHandler(), // GET một kho vũ khí (với ID hay gì đó)
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


    // ========================
    // Concrete handlers
    // ========================
    //Trong trường hợp endpoint not found
    public sealed class NotFoundHandler : IRequestHandler
    {
        public byte[] Handle(string _, Dictionary<string,string> headers) =>
            HttpHelper.Json(404, new { error = "Endpoint Not found" });
    }

    /// <summary>
    /// /api/auth/me
    /// - Trả về thông tin user đang đăng nhập dựa trên token.
    /// - Ưu tiên đọc từ Authorization: Bearer <token>; nếu không có thì fallback cookie 'authToken'.
    /// </summary>
    
    // Helper nhỏ để lấy cookie (nếu cần)
    static class CookieUtil
    {
        public static string? GetCookie(Dictionary<string,string> headers, string name)
        {
            if (!headers.TryGetValue("Cookie", out var cookie) || string.IsNullOrEmpty(cookie)) return null;
            foreach (var part in cookie.Split(';'))
            {
                var kv = part.Split('=', 2, StringSplitOptions.TrimEntries);
                if (kv.Length == 2 && kv[0] == name) return kv[1];
            }
            return null;
        }
    }
    public sealed class MeHandler(IUserRepository repo) : IRequestHandler
    {
        private readonly IUserRepository _repo = repo;

        public byte[] Handle(string body, Dictionary<string, string> headers)
        {
            try
            {
                // 1) Lấy token: Authorization (ưu tiên) hoặc cookie authToken
                headers.TryGetValue("Authorization", out var authHeader);
                if (string.IsNullOrWhiteSpace(authHeader))
                {
                    var cookieToken = CookieUtil.GetCookie(headers, "authToken");
                    if (!string.IsNullOrEmpty(cookieToken))
                        authHeader = "Bearer " + cookieToken;
                }

                // 2) Validate token qua AuthService
                var session = AuthService.Instance.Validate(authHeader);
                if (session is null) return HttpHelper.Json(401, new { error = "Unauthorized" });

                // 3) Lấy profile đầy đủ từ DB (dựa vào Username có trong session)
                var user = _repo.FindByUsernameAsync(session.Username).GetAwaiter().GetResult();
                if (user is null) return HttpHelper.Json(404, new { error = "User not found" });

                // 4) Trả JSON gọn cho UI
                return HttpHelper.Json(200, new
                {
                    id = user.Id,
                    username = user.Username,
                    fullname = user.Fullname,
                    role = user.Role,
                    is_admin = user.IsAdmin,
                    clearance = user.ClearanceLevel
                });
            }
            catch (Exception ex)
            {
                Logger.Log($"[ERROR] MeHandler: {ex}");
                return HttpHelper.Json(500, new { error = "Server error" });
            }
        }
    }

    //Endpoint login (tui đụng rồi nhé)
    public sealed class LoginHandler(IUserRepository repo) : IRequestHandler
    {
        private readonly IUserRepository _repo = repo;

        private sealed record LoginRequest(string Username, string Password);

        public byte[] Handle(string body, Dictionary<string, string> headers)
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
                    new()
                    {
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
                headers.TryGetValue("Authorization", out var authHeader);

                // Revoke the token from memory
                AuthService.Instance.Revoke(authHeader);

                Console.WriteLine($"[INFO] User logged out: {authHeader}");

                // Return JSON + clear the cookie
                return HttpHelper.Json(200, new { ok = true, message = "Logged out" },
                    new()
                    {
                        // overwrite cookie with empty value, expired
                        ["Set-Cookie"] = "auth=; Path=/; Max-Age=0; HttpOnly; SameSite=Lax"
                    });
            }
            catch (Exception ex)
            {
                Logger.Log($"[ERROR] LogoutHandler: {ex.Message}");
                return HttpHelper.Json(500, new { error = "Server error" });
            }
        }
    }
    
    //Handler GET một kho vũ khí
    public sealed class StorageHandler : IRequestHandler
    {
        public byte[] Handle(string body, Dictionary<string, string> headers)
        {
            try
            {
                // Parse the body to get the storage_id
                var doc = JsonDocument.Parse(body);
                var root = doc.RootElement;
                int id = root.GetProperty("storage_id").GetInt32();

                var storage = Database.GetStorageById(id);
                if (storage is null) return HttpHelper.Json(404, new { error = "Storage not found" });
                return HttpHelper.Json(200, storage);
            }
            catch (Exception ex)
            {
                Logger.Log($"[ERROR] StorageHandler: {ex.Message}");
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
                // var sw = System.Diagnostics.Stopwatch.StartNew(); //debug
                var storages = Database.GetAllStorages();
                // sw.Stop();//debug
                // Console.WriteLine($"[PERF] Database.GetAllStorages took {sw.ElapsedMilliseconds}ms");//debug
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
            //chỉ có admin mới làm được cái này
            // if (!AuthService.RequireRole(headers, "Admin"))
            //     return HttpHelper.Json(403, new { error = "Forbidden" });
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

    //Handler cho DELETE kho vũ khí
    public sealed class DeleteStorageHandler : IRequestHandler
    {
        public byte[] Handle(string body, Dictionary<string,string> headers)
        {
            try
            {
                var doc = JsonDocument.Parse(body);
                var root = doc.RootElement;

                int storageId = root.GetProperty("storage_id").GetInt32();

                Database.DeleteStorage(storageId);

                return HttpHelper.Json(204, new { message = "Storage deleted successfully" });
            }
            catch (Exception ex)
            {
                Logger.Log($"[ERROR] DeleteStorageHandler: {ex.Message}");
                return HttpHelper.Json(500, new { error = "Server error" });
            }
        }
    }

    //Handler cho UPDATE kho vũ khí
    public sealed class UpdateStorageHandler : IRequestHandler
    {
        public byte[] Handle(string body, Dictionary<string,string> headers)
        {
            try
            {
                var doc = JsonDocument.Parse(body);
                var root = doc.RootElement;

                int storageId = root.GetProperty("storage_id").GetInt32();
                string locationName = root.GetProperty("location_name").GetString()!;
                decimal latitude = root.GetProperty("latitude").GetDecimal();
                decimal longitude = root.GetProperty("longitude").GetDecimal();

                Database.UpdateStorage(storageId, locationName, latitude, longitude);

                return HttpHelper.Json(200, new { message = "Storage updated successfully" });
            }
            catch (Exception ex)
            {
                Logger.Log($"[ERROR] UpdateStorageHandler: {ex.Message}");
                return HttpHelper.Json(500, new { error = "Server error" });
            }
        }
    }

    //Handler
    public sealed class AllWeaponHandler : IRequestHandler
    {
        public byte[] Handle(string body, Dictionary<string,string> headers)
        {
            try
            {
                var weapons = Database.GetAllWeapon();
                return HttpHelper.Json(200, weapons);
            }
            catch (Exception ex)
            {
                Logger.Log($"[ERROR] AllWeaponHandler: {ex.Message}");
                return HttpHelper.Json(500, new { error = "Server error" });
            }
        }
    }

    public sealed class WeaponHandler : IRequestHandler
    {
        public byte[] Handle(string body, Dictionary<string,string> headers)
        {
            try
            {
                // Parse the body to get the weapon_id
                var doc = JsonDocument.Parse(body);
                var root = doc.RootElement;
                int id = root.GetProperty("weapon_id").GetInt32();

                var weapon = Database.GetWeaponById(id);
                if (weapon is null) return HttpHelper.Json(404, new { error = "Weapon not found" });
                return HttpHelper.Json(200, weapon);
            }
            catch (Exception ex)
            {
                Logger.Log($"[ERROR] WeaponHandler: {ex.Message}");
                return HttpHelper.Json(500, new { error = "Server error" });
            }
        }
    }

    public sealed class AddWeaponHandler : IRequestHandler
    {
        public byte[] Handle(string body, Dictionary<string,string> headers)
        {
            try
            {
                // parse body JSON: { "name": "...", "type": "...", ... }
                var doc = JsonDocument.Parse(body);
                var root = doc.RootElement;

                string name = root.GetProperty("name").GetString()!;
                string type = root.GetProperty("type").GetString()!;
                decimal? yieldMegatons = root.TryGetProperty("yield_megatons", out var y) ? y.GetDecimal() : null;
                int? rangeKm = root.TryGetProperty("range_km", out var r) ? r.GetInt32() : null;
                int? weightKg = root.TryGetProperty("weight_kg", out var w) ? w.GetInt32() : null;
                string status = root.GetProperty("status").GetString()!;
                string countryOfOrigin = root.GetProperty("country_of_origin").GetString()!;
                int? yearCreated = root.TryGetProperty("year_created", out var yc) ? yc.GetInt32() : null;
                string? notes = root.TryGetProperty("notes", out var n) ? n.GetString() : null;

                bool success = Database.AddWeapon(name, type, yieldMegatons, rangeKm, weightKg, status, countryOfOrigin, yearCreated, notes);

                if (success)
                    return HttpHelper.Json(201, new { message = "Weapon added successfully" });
                else
                    return HttpHelper.Json(400, new { error = "Failed to add weapon" });
            }
            catch (Exception ex)
            {
                Logger.Log($"[ERROR] AddWeaponHandler: {ex.Message}");
                return HttpHelper.Json(500, new { error = "Server error" });
            }
        }
    }

    public sealed class DeleteWeaponHandler : IRequestHandler
    {
        public byte[] Handle(string body, Dictionary<string,string> headers)
        {
            try
            {
                var doc = JsonDocument.Parse(body);
                var root = doc.RootElement;

                int weaponId = root.GetProperty("weapon_id").GetInt32();

                Database.DeleteWeapon(weaponId);

                return HttpHelper.Json(204, new { message = "Weapon deleted successfully" });
            }
            catch (Exception ex)
            {
                Logger.Log($"[ERROR] DeleteWeaponHandler: {ex.Message}");
            return HttpHelper.Json(500, new { error = "Server error" });
            }
        }
    }

    public sealed class UpdateWeaponHandler : IRequestHandler
    {
        public byte[] Handle(string body, Dictionary<string,string> headers)
        {
            try
            {
                var doc = JsonDocument.Parse(body);
                var root = doc.RootElement;

                int weaponId = root.GetProperty("weapon_id").GetInt32();
                string name = root.GetProperty("name").GetString()!;
                string type = root.GetProperty("type").GetString()!;
                decimal? yieldMegatons = root.TryGetProperty("yield_megatons", out var y) ? y.GetDecimal() : null;
                int? rangeKm = root.TryGetProperty("range_km", out var r) ? r.GetInt32() : null;
                int? weightKg = root.TryGetProperty("weight_kg", out var w) ? w.GetInt32() : null;
                string status = root.GetProperty("status").GetString()!;
                string countryOfOrigin = root.GetProperty("country_of_origin").GetString()!;
                int? yearCreated = root.TryGetProperty("year_created", out var yc) ? yc.GetInt32() : null;
                string? notes = root.TryGetProperty("notes", out var n) ? n.GetString() : null;

                Database.UpdateWeapon(weaponId, name, type, yieldMegatons, rangeKm, weightKg, status, countryOfOrigin, yearCreated, notes);

                return HttpHelper.Json(200, new { message = "Weapon updated successfully" });
            }
            catch (Exception ex)
            {
                Logger.Log($"[ERROR] UpdateWeaponHandler: {ex.Message}");
                return HttpHelper.Json(500, new { error = "Server error" });
            }
        }
    }
}
