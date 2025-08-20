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
            "/api/auth/logout" => new LogoutHandler(),

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
    //Handler GET một kho vũ khí
    public sealed class StorageHandler : IRequestHandler
    {
        public byte[] Handle(string body)
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

    //Handler cho DELETE kho vũ khí
    public sealed class DeleteStorageHandler : IRequestHandler
    {
        public byte[] Handle(string body)
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
        public byte[] Handle(string body)
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
        public byte[] Handle(string body)
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
        public byte[] Handle(string body)
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
        public byte[] Handle(string body)
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
        public byte[] Handle(string body)
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
        public byte[] Handle(string body)
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
