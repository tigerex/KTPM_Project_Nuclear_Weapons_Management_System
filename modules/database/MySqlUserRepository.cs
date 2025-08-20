using System.Data;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;

namespace project_nuclear_weapons_management_system.modules.database
{
    // Adapter: bọc MySQL dưới interface IUserRepository
    public class MySqlUserRepository : IUserRepository
    {
        public async Task<UserDto?> FindByUsernameAsync(string username)
        {
            using var conn = Database.GetConnection(); // tái dùng lớp Database của bạn
            const string sql = @"
                SELECT 
                    user_id,
                    username,
                    full_name,
                    password_hash,
                    role,
                    is_admin,
                    clearance_level
                FROM users
                WHERE username = @u
                LIMIT 1;";


            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@u", username);

            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new UserDto(
                    Id: reader.GetInt32("user_id"),
                    Username: reader.GetString("username"),
                    Fullname: reader.GetString("full_name"),
                    PasswordHash: reader.GetString("password_hash"),
                    Role: reader.GetString("role"),
                    IsAdmin: reader.GetBoolean("is_admin"),
                    ClearanceLevel: reader.GetString("clearance_level")
                );
            }
            return null;
        }
    }
}
