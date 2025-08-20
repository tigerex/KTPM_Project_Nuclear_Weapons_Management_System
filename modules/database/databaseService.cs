using System;
using MySql.Data.MySqlClient;
using System.Threading.Tasks;
using System.Data;
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
        int? year_craeted,
        string? notes
    );

    /// <summary>
    /// Database module chịu trách nhiệm quản lý kết nối và truy vấn đến MySQL database.
    /// - Sử dụng MySQL Connector/NET (MySql.Data)
    /// - Đảm bảo mở kết nối trước khi truy vấn.
    /// </summary>
    public static class Database
    {
        // Chuỗi kết nối đến MySQL database, nhớ đổi host, pass, database nếu khác tên
        private static string connectionString = "Server=127.0.0.1;Database=nuclear_weapon;User ID=root;Password=1234;";

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
        public static void GetAllWeapon()
        {
            using (var conn = GetConnection())
            {
                string sql = "SELECT weapon_id, name, type FROM weapons;";
                using (var cmd = new MySqlCommand(sql, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        Console.WriteLine($"Weapon: {reader["name"]} (Type: {reader["type"]}, ID: {reader["weapon_id"]})");
                    }
                }
            }
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


        // ======= Chuẩn bị cho login =======

        // public sealed record UserDto(
        //     int Id,
        //     string Username,
        //     string PasswordHash,
        //     string Role,
        //     bool IsAdmin,
        //     string ClearanceLevel
        // );


        // /// <summary>
        // /// Lấy user theo username. Dùng async để không block.
        // /// </summary>
        // public static async Task<UserDto?> GetUserByUsernameAsync(string username)
        // {
        //     using var conn = GetConnection();
        //     const string sql = @"
        //         SELECT 
        //             user_id,
        //             username,
        //             password_hash,
        //             role,
        //             is_admin,
        //             clearance_level
        //         FROM users
        //         WHERE username = @u
        //         LIMIT 1;";


        //     using var cmd = new MySqlCommand(sql, conn);
        //     cmd.Parameters.AddWithValue("@u", username);

        //     using var reader = await cmd.ExecuteReaderAsync();
        //     if (await reader.ReadAsync())
        //     {
        //         return new UserDto(
        //             Id: reader.GetInt32("user_id"),
        //             Username: reader.GetString("username"),
        //             PasswordHash: reader.GetString("password_hash"),
        //             Role: reader.GetString("role"),
        //             IsAdmin: reader.GetBoolean("is_admin"),
        //             ClearanceLevel: reader.GetString("clearance_level")
        //         );
        //     }
        //     return null;
        // }
    }
}
