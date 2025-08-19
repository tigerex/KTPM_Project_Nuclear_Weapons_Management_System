using ProjectNuclearWeaponsManagementSystem.Modules;
using project_nuclear_weapons_management_system.modules.database;
using project_nuclear_weapons_management_system.modules.server;

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




// using System;
// using BCrypt.Net;

// class Tmp {
//   static void Main() {
//     var hash = BCrypt.Net.BCrypt.HashPassword("admin123", workFactor: 12);
//     Console.WriteLine(hash);
//   }
// }
