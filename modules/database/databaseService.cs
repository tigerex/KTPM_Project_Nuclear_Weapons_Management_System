using System;
using MySql.Data.MySqlClient;
using System.Threading.Tasks;
using System.Data;
using System.Dynamic;
// test user admin admin123
namespace project_nuclear_weapons_management_system.modules.database
{
    //khai báo object kho vũ khí
    public sealed record StorageDto(
        int storage_id,
        string location_name,
        decimal latitude,
        decimal longitude,
        DateTime? last_inspection
    );

    //khai báo object vũ khí
    public sealed record WeaponDto(
        int weapon_id,
        string name,
        string type,
        decimal? yield_megatons,
        int? range_km,
        int? weight_kg,
        string status,
        string country_of_origin,
        int? year_created,
        string? notes
    );

    //Object vũ khí trong kho
    public sealed record WeaponInventoryDto(
        int WeaponId,
        string WeaponName,
        string WeaponType,
        int Quantity
    );

    //Object kho có vũ khí
    public sealed record StorageInventoryDto(
        int StorageId,
        string StorageName,
        List<WeaponInventoryDto> Weapons
    );



    /// <summary>
    /// Database module chịu trách nhiệm quản lý kết nối và truy vấn đến MySQL database.
    /// - Sử dụng MySQL Connector/NET (MySql.Data)
    /// - Đảm bảo mở kết nối trước khi truy vấn.
    /// </summary>
    public static class Database
    {
        // Chuỗi kết nối đến MySQL database, nhớ đổi host, pass, database nếu khác tên
        private static string connectionString = "Server=localhost;Database=nuclear_weapon;User ID=root;Password=gunnyvip2003;";

        /// <summary>
        /// Trả về một MySqlConnection đã mở sẵn.
        /// - Nếu kết nối thất bại, sẽ hiển thị lỗi trên console.
        /// </summary>
        /// <returns>MySqlConnection đã mở</returns>
        public static MySqlConnection GetConnection()
        {
            var conn = new MySqlConnection(connectionString);
            try
            {
                conn.Open();
                Console.WriteLine("✅ Database connection established.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ Database connection failed: " + ex.Message);
            }
            return conn;
        }

        // Ví dụ truy vấn test: lấy danh sách vũ khí từ bảng weapons.
        public static List<WeaponDto> GetAllWeapon()
        {
            var weapons = new List<WeaponDto>();
            using (var conn = GetConnection())
            {
                string sql = @"SELECT weapon_id, name, type, yield_megatons, range_km, weight_kg, status, country_of_origin, year_created, notes FROM weapons;";

                using (var cmd = new MySqlCommand(sql, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        weapons.Add(new WeaponDto(
                            weapon_id: reader.GetInt32("weapon_id"),
                            name: reader.GetString("name"),
                            type: reader.GetString("type"),
                            yield_megatons: reader.IsDBNull("yield_megatons") ? null : reader.GetDecimal("yield_megatons"),
                            range_km: reader.IsDBNull("range_km") ? null : reader.GetInt32("range_km"),
                            weight_kg: reader.IsDBNull("weight_kg") ? null : reader.GetInt32("weight_kg"),
                            status: reader.GetString("status"),
                            country_of_origin: reader.GetString("country_of_origin"),
                            year_created: reader.IsDBNull("year_created") ? null : reader.GetInt32("year_created"),
                            notes: reader.IsDBNull("notes") ? null : reader.GetString("notes")
                        ));
                    }
                }
            }
            return weapons;
        }

        //GET danh sách tất cả các kho vũ khí.
        public static List<StorageDto> GetAllStorages()
        {
            var storages = new List<StorageDto>();

            using (var conn = GetConnection())
            {
                const string sql = @"SELECT storage_id, location_name, latitude, longitude, last_inspection FROM storages;";

                using var cmd = new MySqlCommand(sql, conn);
                using var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    storages.Add(new StorageDto(
                        storage_id: reader.GetInt32("storage_id"),
                        location_name: reader.GetString("location_name"),
                        latitude: reader.GetDecimal("latitude"),
                        longitude: reader.GetDecimal("longitude"),
                        last_inspection: reader.IsDBNull("last_inspection")
                            ? null
                            : reader.GetDateTime("last_inspection")
                    ));
                }
            }

            return storages;
        }

        //POST thêm kho vũ khí
        public static int AddStorage(string location_name, decimal latitude, decimal longitude)
        {
            using var conn = GetConnection();

            const string sql = @"INSERT INTO storages (location_name, latitude, longitude, last_inspection) VALUES (@name, @lat, @lng, NOW()); SELECT LAST_INSERT_ID();";

            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@name", location_name);
            cmd.Parameters.AddWithValue("@lat", latitude);
            cmd.Parameters.AddWithValue("@lng", longitude);

            // ExecuteScalar trả về object → ép sang int
            var id = Convert.ToInt32(cmd.ExecuteScalar());
            return id;
        }

        public static StorageDto? GetStorageById(int id)
        {
            using var conn = GetConnection();

            const string sql = @"SELECT storage_id, location_name, latitude, longitude, last_inspection FROM storages WHERE storage_id = @id LIMIT 1;";

            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", id);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                return new StorageDto(
                    storage_id: reader.GetInt32("storage_id"),
                    location_name: reader.GetString("location_name"),
                    latitude: reader.GetDecimal("latitude"),
                    longitude: reader.GetDecimal("longitude"),
                    last_inspection: reader.IsDBNull("last_inspection")
                        ? null
                        : reader.GetDateTime("last_inspection")
                );
            }
            return null;
        }

        // PUT kho vũ khí
        public static bool UpdateStorage(int id, string locationName, decimal latitude, decimal longitude)
        {
            using var conn = GetConnection();

            const string sql = @"UPDATE storages SET location_name = @name, latitude = @lat, longitude = @lng WHERE storage_id = @id;";

            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", id);
            cmd.Parameters.AddWithValue("@name", locationName);
            cmd.Parameters.AddWithValue("@lat", latitude);
            cmd.Parameters.AddWithValue("@lng", longitude);

            return cmd.ExecuteNonQuery() > 0; // Trả về true nếu có bản ghi được cập nhật
        }

        // DELETE kho vũ khí
        public static bool DeleteStorage(int id)
        {
            if (id <= 0) return false;

            using var conn = GetConnection();
            using var tran = conn.BeginTransaction();
            try
            {
                // B1: Xóa tất cả inventory liên quan tới storage này
                using (var cmd1 = new MySqlCommand("DELETE FROM storage_inventory WHERE storage_id = @id;", conn, tran))
                {
                    cmd1.Parameters.Add("@id", MySqlDbType.Int32).Value = id;
                    cmd1.ExecuteNonQuery();
                }
                // B2: Xóa chính record trong storages
                using (var cmd2 = new MySqlCommand("DELETE FROM storages WHERE storage_id = @id;", conn, tran))
                {
                    cmd2.Parameters.Add("@id", MySqlDbType.Int32).Value = id;
                    int rows = cmd2.ExecuteNonQuery();
                    tran.Commit();
                    return rows > 0; // true nếu xóa thành công ít nhất 1 dòng
                }
            }
            catch
            {
                tran.Rollback();
                throw; // quăng lỗi ra ngoài nếu có sự cố
            }
        }

        // GET 1 vũ khí
        public static WeaponDto? GetWeaponById(int id)
        {
            using var conn = GetConnection();

            const string sql = @"SELECT weapon_id, name, type, yield_megatons, range_km, weight_kg, status, country_of_origin, year_created, notes
                                    FROM weapons 
                                    WHERE weapon_id = @id 
                                    LIMIT 1;";

            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", id);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                return new WeaponDto(
                    reader.GetInt32("weapon_id"),
                    reader.GetString("name"),
                    reader.GetString("type"),
                    reader.IsDBNull("yield_megatons") ? null : reader.GetDecimal("yield_megatons"),
                    reader.IsDBNull("range_km") ? null : reader.GetInt32("range_km"),
                    reader.IsDBNull("weight_kg") ? null : reader.GetInt32("weight_kg"),
                    reader.GetString("status"),
                    reader.GetString("country_of_origin"),
                    reader.IsDBNull("year_created") ? null : reader.GetInt32("year_created"),
                    reader.IsDBNull("notes") ? null : reader.GetString("notes")
                );
            }
            return null;
        }

        // POST vũ khí
        public static bool AddWeapon(
            string name,
            string type,
            decimal? yieldMegatons,
            int? rangeKm,
            int? weightKg,
            string status,
            string countryOfOrigin,
            int? yearCreated,
            string? notes)
        {
            using var conn = GetConnection();

            const string sql = @"
                                INSERT INTO weapons
                                    (name, type, yield_megatons, range_km, weight_kg, status, country_of_origin, year_created, notes)
                                VALUES
                                    (@name, @type, @yield_megatons, @range_km, @weight_kg, @status, @country_of_origin, @year_created, @notes);";

            using var cmd = new MySqlCommand(sql, conn);

            cmd.Parameters.AddWithValue("@name", name);
            cmd.Parameters.AddWithValue("@type", type);
            cmd.Parameters.AddWithValue("@yield_megatons", (object?)yieldMegatons ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@range_km", (object?)rangeKm ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@weight_kg", (object?)weightKg ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@status", status);
            cmd.Parameters.AddWithValue("@country_of_origin", countryOfOrigin);
            cmd.Parameters.AddWithValue("@year_created", (object?)yearCreated ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@notes", (object?)notes ?? DBNull.Value);

            int rows = cmd.ExecuteNonQuery();
            return rows > 0;
        }

        //DELETE vũ khí
        public static bool DeleteWeapon(int id)
        {
            using var conn = GetConnection();

            const string sql = @"DELETE FROM weapons WHERE weapon_id = @id;";

            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", id);

            return cmd.ExecuteNonQuery() > 0; // Trả về true nếu có bản ghi bị xóa
        }

        //PUT vũ khí
        public static bool UpdateWeapon(int id, string name, string type, decimal? yieldMegatons, int? rangeKm, int? weightKg, string status, string countryOfOrigin, int? yearCreated, string? notes)
        {
            using var conn = GetConnection();

            const string sql = @"
                                    UPDATE weapons
                                    SET
                                        name = @name,
                                        type = @type,
                                        yield_megatons = @yield_megatons,
                                        range_km = @range_km,
                                        weight_kg = @weight_kg,
                                        status = @status,
                                        country_of_origin = @country_of_origin,
                                        year_created = @year_created,
                                        notes = @notes
                                    WHERE weapon_id = @id;";

            using var cmd = new MySqlCommand(sql, conn);

            cmd.Parameters.AddWithValue("@id", id);
            cmd.Parameters.AddWithValue("@name", name);
            cmd.Parameters.AddWithValue("@type", type);
            cmd.Parameters.AddWithValue("@yield_megatons", (object?)yieldMegatons ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@range_km", (object?)rangeKm ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@weight_kg", (object?)weightKg ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@status", status);
            cmd.Parameters.AddWithValue("@country_of_origin", countryOfOrigin);
            cmd.Parameters.AddWithValue("@year_created", (object?)yearCreated ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@notes", (object?)notes ?? DBNull.Value);

            int rows = cmd.ExecuteNonQuery();
            return rows > 0;
        }

        //GET tất cả vũ khí trong các kho
        public static List<StorageInventoryDto> GetAllInventories()
        {
            var storages = new Dictionary<int, StorageInventoryDto>();

            using var conn = GetConnection();
            const string sql = @"
                                    SELECT 
                                        s.storage_id,
                                        s.location_name,
                                        w.weapon_id,
                                        w.name AS weapon_name,
                                        w.type AS weapon_type,
                                        si.quantity
                                    FROM storage_inventory si
                                    JOIN storages s ON si.storage_id = s.storage_id
                                    JOIN weapons w ON si.weapon_id = w.weapon_id
                                    ORDER BY s.storage_id, w.weapon_id;
                                ";

            using var cmd = new MySqlCommand(sql, conn);
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                int storageId = reader.GetInt32("storage_id");

                if (!storages.ContainsKey(storageId))
                {
                    storages[storageId] = new StorageInventoryDto(
                        StorageId: storageId,
                        StorageName: reader.GetString("location_name"),
                        Weapons: new List<WeaponInventoryDto>()
                    );
                }

                storages[storageId].Weapons.Add(new WeaponInventoryDto(
                    WeaponId: reader.GetInt32("weapon_id"),
                    WeaponName: reader.GetString("weapon_name"),
                    WeaponType: reader.GetString("weapon_type"),
                    Quantity: reader.GetInt32("quantity")
                ));
            }

            return storages.Values.ToList();
        }

        //GET tất cả vũ khí từ 1 kho theo ID
        public static StorageInventoryDto? GetInventoryByStorageId(int storageId)
        {
            StorageInventoryDto? storage = null;

            using var conn = GetConnection();
            const string sql = @"
                                    SELECT 
                                        s.storage_id,
                                        s.location_name,
                                        w.weapon_id,
                                        w.name AS weapon_name,
                                        w.type AS weapon_type,
                                        si.quantity
                                    FROM storage_inventory si
                                    JOIN storages s ON si.storage_id = s.storage_id
                                    JOIN weapons w ON si.weapon_id = w.weapon_id
                                    WHERE s.storage_id = @storageId
                                    ORDER BY w.weapon_id;
                                ";

            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@storageId", storageId);
            Console.WriteLine("Database: " + storageId);
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                if (storage == null)
                {
                    storage = new StorageInventoryDto(
                        StorageId: reader.GetInt32("storage_id"),
                        StorageName: reader.GetString("location_name"),
                        Weapons: new List<WeaponInventoryDto>()
                    );
                }

                storage.Weapons.Add(new WeaponInventoryDto(
                    WeaponId: reader.GetInt32("weapon_id"),
                    WeaponName: reader.GetString("weapon_name"),
                    WeaponType: reader.GetString("weapon_type"),
                    Quantity: reader.GetInt32("quantity")
                ));
            }

            return storage;
        }

        //PUT sửa invenroty 
        public static bool UpdateInventory(int storageId, List<(int WeaponId, int Quantity)> weapons)
        {
            using var conn = GetConnection();
            using var tx = conn.BeginTransaction();

            try
            {
                // Xóa inventory cũ
                const string deleteSql = "DELETE FROM storage_inventory WHERE storage_id = @sid;";
                using (var del = new MySqlCommand(deleteSql, conn, tx))
                {
                    del.Parameters.AddWithValue("@sid", storageId);
                    del.ExecuteNonQuery();
                }

                // Chèn inventory mới
                const string insertSql = @"INSERT INTO storage_inventory (storage_id, weapon_id, quantity) 
                                        VALUES (@sid, @wid, @qty);";
                foreach (var (weaponId, qty) in weapons)
                {
                    using var ins = new MySqlCommand(insertSql, conn, tx);
                    ins.Parameters.AddWithValue("@sid", storageId);
                    ins.Parameters.AddWithValue("@wid", weaponId);
                    ins.Parameters.AddWithValue("@qty", qty);
                    ins.ExecuteNonQuery();
                }

                tx.Commit();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] UpdateInventory failed: {ex.Message}");
                tx.Rollback();
                return false;
            }
        }

        // GET all users
        public static List<UserDto> GetAllUsers()
        {
            var users = new List<UserDto>();
            using var conn = GetConnection();
            const string sql = @"
                SELECT 
                    user_id,
                    username,
                    full_name,
                    password_hash,
                    role,
                    is_admin,
                    clearance_level
                FROM users;";

            using var cmd = new MySqlCommand(sql, conn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                users.Add(new UserDto(
                    Id: reader.GetInt32("user_id"),
                    Username: reader.GetString("username"),
                    Fullname: reader.GetString("full_name"),
                    PasswordHash: reader.GetString("password_hash"),
                    Role: reader.GetString("role"),
                    IsAdmin: reader.GetBoolean("is_admin"),
                    ClearanceLevel: reader.GetString("clearance_level")
                ));
            }
            return users;
        }

        // POST user
        public static int AddUser(
            string username,
            string passwordHash,
            string fullName,
            string role,
            string? country,
            string? organization,
            string clearanceLevel,
            bool isAdmin
        )
        {
            using var conn = GetConnection();
            const string sql = @"
                INSERT INTO users 
                    (username, password_hash, full_name, role, country, organization, clearance_level, is_admin) 
                VALUES 
                    (@username, @password_hash, @full_name, @role, @country, @organization, @clearance_level, @is_admin);
                SELECT LAST_INSERT_ID();";

            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@username", username);
            cmd.Parameters.AddWithValue("@password_hash", passwordHash);
            cmd.Parameters.AddWithValue("@full_name", fullName);
            cmd.Parameters.AddWithValue("@role", role);
            cmd.Parameters.AddWithValue("@country", (object?)country ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@organization", (object?)organization ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@clearance_level", clearanceLevel);
            cmd.Parameters.AddWithValue("@is_admin", isAdmin);

            var result = cmd.ExecuteScalar();
            return Convert.ToInt32(result); // return the new user_id
        }

    }
}
