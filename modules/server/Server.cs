using System;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;

/// <summary>
/// Server chịu trách nhiệm khởi chạy một TCP server trên IP Wi-Fi của máy.
/// Server này:
/// - Lắng nghe kết nối từ client.
/// - Log tất cả kết nối, disconnection, request và lỗi thông qua Logger.
/// - Hỗ trợ dừng server có delay hoặc Ctrl+C.
/// - Phản hồi các request HTTP đơn giản với HTML.
/// </summary>
namespace ProjectNuclearWeaponsManagementSystem.Modules.Server
{
    public static class ServerModule
    {

        private static TcpListener? _listener; // TCP listener
        private static bool _isRunning = false; // trạng thái server đang chạy
        private static bool _stopRequested = false; // flag dừng server
        private static DateTime _stopTime;  // thời điểm server dừng (nếu có)

        // Khởi tạo class static để gọi từ global
        static ServerModule()
        {
            // Xử lý Ctrl+C (CancelKeyPress) event
            Console.CancelKeyPress += (sender, e) =>
            {
                e.Cancel = true; // tránh terminate ngay
                if (_isRunning)
                {
                    int delaySeconds = 10;
                    Logger.Log("[ALERT] Server stopping due to Ctrl+C / CancelKeyPress");
                    Console.WriteLine("[ALERT] Server stopping in 10 seconds");
                    _stopRequested = true;
                    _stopTime = DateTime.Now.AddSeconds(delaySeconds);
                    // Stop();
                }
            };

            // Ghi log khi process kết thúc
            AppDomain.CurrentDomain.ProcessExit += (sender, e) =>
            {
                Logger.Log("[INFO] Server shut down due to ProcessExit (application end).");
                Logger.Log("===== SESSION END =====\n");
                // Don't call Stop() here; process is dying anyway
            };

            // Bắt lỗi không handle được
            AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
            {
                if (e.ExceptionObject is Exception ex)
                {
                    Logger.Log($"[ERROR] Server crashed with unhandled exception: {ex.Message}\n{ex.StackTrace}");
                    Logger.Log("===== SESSION END =====\n");
                }
                else
                {
                    Logger.Log("[ERROR] Server crashed with unhandled exception: ExceptionObject is NULL.");
                    Logger.Log("===== SESSION END =====\n");
                }
                if (_isRunning)
                {
                    Stop();
                }
            };
        }

        /// <summary>
        /// Bắt đầu server TCP trên IP Wi-Fi tại port 9999.
        /// Server này:
        /// - Lắng nghe kết nối từ client.
        /// - Ghi log tất cả kết nối, disconnection, request và lỗi thông qua Logger.
        /// - Phản hồi HTTP request với nội dung HTML cơ bản.
        /// - Hỗ trợ dừng server theo lệnh console hoặc Ctrl+C.
        /// </summary>
        public static void Start()
        {
            try
            {
                // Lấy IP Wi-Fi hiện tại
                string localIP = NetworkHelper.GetLocalWiFiIPAddress();
                int port = 9999;

                _listener = new TcpListener(IPAddress.Parse(localIP), port);
                _listener.Start();
                _isRunning = true;

                Logger.Log($"✅ Server started successfully at {localIP}:{port}");
                Logger.Log($"====== SESSION ACTIVITIES =====");
                Console.WriteLine($"Connect at http://{localIP}:{port}\n");
                Console.WriteLine("Type 'stop' or 'stop N' to stop the server (N = delay in seconds| default 10 seconds).");

                // Vòng lặp chính của server
                while (_isRunning)
                {
                    // --- 1. Kiểm tra lệnh từ console ---
                    if (Console.KeyAvailable)
                    {
                        string? command = Console.ReadLine();
                        Logger.Log("!!!Command issue!!!");
                        if (command != null && command.StartsWith("stop"))
                        {
                            string arg = command.Replace("stop", "").Trim(' ', '(', ')');
                            int delaySeconds = 10; // default
                            if (int.TryParse(arg, out int parsedDelay))
                                delaySeconds = parsedDelay;

                            _stopRequested = true;
                            _stopTime = DateTime.Now.AddSeconds(delaySeconds);

                            Logger.Log($"[ALERT] Server will stop in {delaySeconds} seconds...");
                            Console.WriteLine($"[ALERT] Server will stop in {delaySeconds} seconds...");
                        }
                    }

                    // --- 2. Kiểm tra xem có lệnh stop đã đến thời gian dừng ---
                    if (_stopRequested && DateTime.Now >= _stopTime)
                    {
                        Stop();
                    }

                    // --- 3. Kiểm tra nếu có client kết nối đến ---
                    if (_isRunning && _listener != null && _listener.Pending())
                    {
                        TcpClient client = _listener.AcceptTcpClient();
                        string clientIP = ((IPEndPoint)client.Client.RemoteEndPoint!).Address.ToString();
                        Logger.Log($"Client connected from {clientIP}");

                        try
                        {
                            using NetworkStream stream = client.GetStream();
                            stream.ReadTimeout = 30000; // 30s timeout

                            // --- 4. Xử lý request từ client ---
                            while (_isRunning && client.Connected)
                            {
                                byte[] buffer = new byte[4096];
                                int bytesRead;

                                try
                                {
                                    bytesRead = stream.Read(buffer, 0, buffer.Length);
                                }
                                catch (IOException)
                                {
                                    // timeout -> kết thúc keep-alive
                                    Logger.Log($"[INFO] Keep-alive timeout for {clientIP}, closing connection.");
                                    break;
                                }

                                if (bytesRead == 0)
                                {
                                    // client đóng kết nối mà không gửi request
                                    Logger.Log($"[INFO] Idle client {clientIP} disconnected (no request).");
                                    break;
                                }

                                string requestText = System.Text.Encoding.UTF8.GetString(buffer, 0, bytesRead);
                                string requestLine = requestText.Split("\r\n")[0];
                                Logger.Log($"[DEBUG] Request line from {clientIP}: {requestLine}");

                                // --- 5. Xác định nội dung phản hồi ---
                                string body;
                                string contentType;

                                if (requestLine.Contains("favicon.ico"))
                                {
                                    body = ""; // favicon rỗng, thêm sau
                                    contentType = "image/x-icon";
                                }
                                else
                                {
                                    body = "<h1>hello from project N</h1>";
                                    contentType = "text/html; charset=UTF-8";
                                }

                                // --- 6. Tạo HTTP response ---
                                string httpResponse =
                                    "HTTP/1.1 200 OK\r\n" +
                                    $"Content-Type: {contentType}\r\n" +
                                    $"Content-Length: {System.Text.Encoding.UTF8.GetByteCount(body)}\r\n" +
                                    "Connection: keep-alive\r\n" +
                                    "Keep-Alive: timeout=5, max=100\r\n" +
                                    "\r\n" +
                                    body;

                                // --- 7. Gửi phản hồi tới client ---
                                byte[] responseBytes = System.Text.Encoding.UTF8.GetBytes(httpResponse);
                                stream.Write(responseBytes, 0, responseBytes.Length);
                                stream.Flush();
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.Log($"[ERROR] Failed to process request from {clientIP}: {ex.Message}");
                        }
                        finally
                        {
                            // --- 8. Đóng kết nối client ---                            
                            client.Close();
                            Logger.Log($"Client {clientIP} disconnected.");
                        }
                    }

                    System.Threading.Thread.Sleep(50); // giảm tải CPU
                }
                Console.WriteLine("🛑 Server stopped.");
                Logger.Log("🛑 Server stopped gracefully.");
            }
            catch (Exception ex)
            {
                if (_isRunning)
                {
                    Logger.Log($"❌ Server error: {ex.Message}\n");
                }
                else
                {
                    Logger.Log("🛑 Server stopped.");
                }
            }
        }

        /// <summary>
        /// Dừng server một cách an toàn.
        /// - Thay đổi trạng thái _isRunning thành false.
        /// - Dừng TcpListener nếu còn đang chạy.
        /// - Bắt và bỏ qua ObjectDisposedException nếu listener đã bị dispose.
        /// </summary>
        public static void Stop()
        {
            if (_isRunning)
            {
                _isRunning = false; // đánh dấu server không còn chạy
                try
                {
                    _listener?.Stop(); // dừng listener TCP
                }
                catch (ObjectDisposedException) { /* safe to ignore: listener đã bị dispose */ }
                finally
                {
                    // Có thể log nếu muốn: Logger.Log("🛑 Server stopped.");
                }
            }
        }
    }
}
