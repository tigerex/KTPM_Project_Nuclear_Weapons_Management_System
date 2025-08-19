using System.Threading.Tasks;

namespace project_nuclear_weapons_management_system.modules.database
{
    public sealed class UserDto
    {
        public UserDto(int Id, string Username, string PasswordHash, string Role, bool IsAdmin, string ClearanceLevel)
        {
            this.Id = Id;
            this.Username = Username;
            this.PasswordHash = PasswordHash;
            this.Role = Role;
            this.IsAdmin = IsAdmin;
            this.ClearanceLevel = ClearanceLevel;
        }

        public int Id { get; init; }
        public string Username { get; init; } = "";
        public string PasswordHash { get; init; } = "";
        public string Role { get; init; } = "";
        public bool IsAdmin { get; init; }
        public string ClearanceLevel { get; init; } = "";
    }

    public interface IUserRepository
    {
        Task<UserDto?> FindByUsernameAsync(string username);
    }
}
