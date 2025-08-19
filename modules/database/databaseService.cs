using System;
using MySql.Data.MySqlClient;
using System.Threading.Tasks;
using System.Data;

namespace project_nuclear_weapons_management_system.modules.database
{
    /// <summary>
    /// Database chịu trách nhiệm quản lý kết nối và truy vấn đến MySQL database.
    /// - Sử dụng MySQL Connector/NET (MySql.Data)
    /// - Đảm bảo mở kết nối trước khi truy vấn.
    /// - Có thể mở rộng các method CRUD khác dựa trên GetConnection().
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

        /// <summary>
        /// Ví dụ truy vấn test: lấy danh sách vũ khí từ bảng weapons.
        /// - Mở connection thông qua GetConnection().
        /// - Thực thi query SELECT và in ra console.
        /// - Dùng using để đảm bảo connection, command, reader được dispose tự động.
        /// </summary>
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
