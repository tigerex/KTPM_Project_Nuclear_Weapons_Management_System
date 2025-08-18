using System;
using System.IO;

namespace ProjectNuclearWeaponsManagementSystem.Modules.Server
{
    /// <summary>
    /// Logger chịu trách nhiệm ghi log các sự kiện của server vào file.
    /// - Tự động tạo thư mục "data" nếu chưa tồn tại.
    /// - Ghi log theo định dạng thời gian [yyyy-MM-dd HH:mm:ss].
    /// - Thread-safe: sử dụng lock để tránh ghi log đồng thời gây lỗi.
    /// </summary>
    public static class Logger
    {
        // Đường dẫn file log
        private static readonly string logFilePath = Path.Combine("data", "server.log");
        // Lock để tránh xung đột khi nhiều thread ghi log cùng lúc
        private static readonly object _lock = new object();

        /// <summary>
        /// Constructor static: đảm bảo thư mục "data" tồn tại trước khi ghi log.
        /// </summary>
        static Logger()
        {
            Directory.CreateDirectory("data");
        }

        /// <summary>
        /// Ghi một thông điệp vào file log.
        /// - Thêm timestamp trước message.
        /// - Ghi thread-safe để tránh lỗi khi nhiều client cùng ghi log.
        /// </summary>
        /// <param name="message">Thông điệp cần ghi log</param>
        public static void Log(string message)
        {
            lock (_lock) // đảm bảo chỉ một thread ghi log tại một thời điểm
            {
                string logMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}";
                //Console.WriteLine(logMessage);
                File.AppendAllText(logFilePath, logMessage + Environment.NewLine); // Ghi log vào file, thêm newline
            }
        }
    }
}
