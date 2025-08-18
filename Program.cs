using ProjectNuclearWeaponsManagementSystem.Modules;
using ProjectNuclearWeaponsManagementSystem.Modules.DatabaseService;
using ProjectNuclearWeaponsManagementSystem.Modules.Server;

namespace ProjectNuclearWeaponsManagementSystem
{
    /// <summary>
    /// Entry point của ứng dụng Project Nuclear Weapons Management System.
    /// - Chỗ này gọi module là chính.
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {
            // --- Ví dụ sử dụng database ---
            // Mở comment nếu muốn test query database
            // Database.TestQuery();

            // --- Ví dụ tạo và phóng missile ---
            Missile m1 = new Missile("Peacekeeper");
            m1.Launch();

            // --- Khởi động server TCP ---
            ServerModule.Start();
        }
    }
}
