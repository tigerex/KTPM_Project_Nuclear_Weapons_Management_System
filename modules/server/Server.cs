using System;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;
using project_nuclear_weapons_management_system.modules.database;

using System.Text;
using System.Text.Json;
using System.Collections.Generic;
using BCryptNet = BCrypt.Net.BCrypt;

/// <summary>
/// Server chịu trách nhiệm khởi chạy một TCP server trên IP Wi-Fi của máy.
/// Server này:
/// - Lắng nghe kết nối từ client.
/// - Log tất cả kết nối, disconnection, request và lỗi thông qua Logger.
/// - Hỗ trợ dừng server có delay hoặc Ctrl+C.
/// - Phản hồi các request HTTP đơn giản với HTML.
/// </summary>
namespace project_nuclear_weapons_management_system.modules.server
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
                                byte[] first = new byte[4096];
                                int bytesRead;

                                try
                                {
                                    bytesRead = stream.Read(first, 0, first.Length);
                                }
                                catch (IOException)
                                {
                                    Logger.Log($"[INFO] Keep-alive timeout for {clientIP}, closing connection.");
                                    break;
                                }

                                if (bytesRead == 0)
                                {
                                    Logger.Log($"[INFO] Idle client {clientIP} disconnected (no request).");
                                    break;
                                }

                                var (method, path, headers, body) = ParseHttpRequest(stream, first, bytesRead);
                                Logger.Log($"[DEBUG] {clientIP} -> {method} {path}");

                                // string response;
                                // if (method == "POST" && path == "/api/auth/login")
                                // {
                                //     response = HandleLogin(body);
                                // }
                                // else if (path.Contains("favicon.ico"))
                                // {
                                //     response = BuildHttpResponse(200, "image/x-icon", "");
                                // }
                                // else
                                // {
                                //     // Trang mặc định (demo)
                                //     var html = "<h1>hello from project N</h1>";
                                //     response = BuildHttpResponse(200, "text/html; charset=UTF-8", html);
                                // }

                                // Factory Method + Adapter
                                var repo = new MySqlUserRepository();
                                IRequestHandler handler = HandlerFactory.Create(path, repo);
                                string response = handler.Handle(body);

                                // Gửi phản hồi
                                byte[] responseBytes = Encoding.UTF8.GetBytes(response);
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

  
        // Đọc đầy đủ 1 HTTP request (header + body) từ stream
        private static (string Method, string Path, Dictionary<string, string> Headers, string Body)
        ParseHttpRequest(NetworkStream stream, byte[] firstChunk, int bytesRead)
        {
            var buf = new List<byte>(firstChunk.AsSpan(0, bytesRead).ToArray());

            // Tìm \r\n\r\n kết thúc header
            int headerEnd = FindCrlfCrlf(buf);
            while (headerEnd == -1)
            {
                byte[] tmp = new byte[4096];
                int n = stream.Read(tmp, 0, tmp.Length);
                if (n <= 0) break;
                buf.AddRange(tmp.AsSpan(0, n).ToArray());
                headerEnd = FindCrlfCrlf(buf);
            }

            if (headerEnd == -1)
                return ("", "", new(), "");

            // headerEnd trả về chỉ số NGAY SAU "\r\n\r\n"
            string headerText = Encoding.UTF8.GetString(buf.GetRange(0, headerEnd - 4).ToArray());
            var lines = headerText.Split("\r\n", StringSplitOptions.RemoveEmptyEntries);
            var parts = lines[0].Split(' ', 3, StringSplitOptions.RemoveEmptyEntries);

            string method = parts[0];
            string path = parts[1];

            var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 1; i < lines.Length; i++)
            {
                int idx = lines[i].IndexOf(':');
                if (idx > 0)
                    headers[lines[i][..idx].Trim()] = lines[i][(idx + 1)..].Trim();
            }

            int contentLength = 0;
            if (headers.TryGetValue("Content-Length", out var clStr))
                int.TryParse(clStr, out contentLength);

            // Lấy body (có thể còn thiếu => đọc tiếp)
            int have = buf.Count - headerEnd;
            var bodyBytes = new byte[contentLength];
            if (contentLength > 0)
            {
                int toCopy = Math.Min(contentLength, have);
                if (toCopy > 0)
                    buf.CopyTo(headerEnd, bodyBytes, 0, toCopy);

                int remaining = contentLength - toCopy;
                int offset = toCopy;
                while (remaining > 0)
                {
                    int n = stream.Read(bodyBytes, offset, remaining);
                    if (n <= 0) break;
                    offset += n;
                    remaining -= n;
                }
            }

            string body = contentLength > 0 ? Encoding.UTF8.GetString(bodyBytes) : "";
            return (method, path, headers, body);
        }

        private static int FindCrlfCrlf(List<byte> data)
        {
            for (int i = 0; i <= data.Count - 4; i++)
            {
                if (data[i] == 13 && data[i + 1] == 10 && data[i + 2] == 13 && data[i + 3] == 10)
                    return i + 4; // index ngay sau \r\n\r\n
            }
            return -1;
        }

    }
}
