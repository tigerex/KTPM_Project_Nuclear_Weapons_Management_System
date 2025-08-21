using System.Text;
using project_nuclear_weapons_management_system.modules.database;

namespace project_nuclear_weapons_management_system.modules.server
{
    public static class Router
    {
        public static byte[] Resolve(string path, string body, Dictionary<string,string> headers)
        {
            if (path.StartsWith("/api/"))
            {
                var repo = new MySqlUserRepository();
                var handler = HandlerFactory.Create(path, repo);
                return handler.Handle(body, headers);
            }
            else
            {
                return HandleStatic(path, headers);
            }
        }

        // --- Auth helpers ---
        private static bool IsProtectedPath(string path)
        {
            // thêm route phải login mới vào được vô đây
            return path.StartsWith("/home") || path.StartsWith("/admin") || path.StartsWith("/detail") || path.StartsWith("/profile");
        }

        private static string? GetCookie(Dictionary<string,string> headers, string name)
        {
            if (!headers.TryGetValue("Cookie", out var cookie) || string.IsNullOrEmpty(cookie))
                return null;

            foreach (var part in cookie.Split(';'))
            {
                var kv = part.Split('=', 2, StringSplitOptions.TrimEntries);
                if (kv.Length == 2 && kv[0] == name)
                    return kv[1];
            }
            return null;
        }

        // --- File tĩnh ---
        private static byte[] HandleStatic(string path, Dictionary<string, string> headers)
        {
            // 🚨 enforce authentication for protected pages
            if (IsProtectedPath(path))
            {
                var token = GetCookie(headers, "auth");
                bool ok = !string.IsNullOrEmpty(token) &&
                          AuthService.Instance.Validate($"Bearer {token}") != null;

                if (!ok)
                {
                    // redirect to login if not authenticated
                    return HttpHelper.Redirect("/login");
                }
            }

            // 🚨 if already logged in, don’t show login page again
            if (path.StartsWith("/login"))
            {
                var token = GetCookie(headers, "auth");
                bool ok = !string.IsNullOrEmpty(token) &&
                        AuthService.Instance.Validate($"Bearer {token}") != null;

                if (ok)
                {
                    // already logged in → skip login page
                    return HttpHelper.Redirect("/home");
                }
            }

            //default route
            if (path == "/" || string.IsNullOrEmpty(path))
                path = "/default";

            // Console.WriteLine("Requested: " + path);
            path = path.Replace("..", "");
            string filePath;

            if (path.StartsWith("/data/"))
            {
                filePath = Path.Combine(path.TrimStart('/'));
            }
            else if (!Path.HasExtension(path))
            {
                string folder = path.TrimStart('/');
                filePath = Path.Combine("pages", folder, folder + ".html");
            }
            else
            {
                string ext = Path.GetExtension(path).ToLowerInvariant();
                if ((ext == ".css" || ext == ".js") && path.Count(c => c == '/') == 1)
                {
                    string name = Path.GetFileNameWithoutExtension(path);
                    filePath = Path.Combine("pages", name, Path.GetFileName(path));
                }
                else
                {
                    filePath = Path.Combine("pages", path.TrimStart('/'));
                }
            }

            if (!File.Exists(filePath))
            {
                filePath = Path.Combine("pages", "default", "default.html");
                if (!File.Exists(filePath))
                    return HttpHelper.Raw(404, "text/plain", Encoding.UTF8.GetBytes("Default page not found"));
            }

            string ext2 = Path.GetExtension(filePath).ToLowerInvariant();
            string contentType = ext2 switch
            {
                ".html" => "text/html",
                ".css" => "text/css",
                ".js" => "application/javascript",
                ".png" => "image/png",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".gif" => "image/gif",
                ".svg" => "image/svg+xml",
                _ => "application/octet-stream"
            };

            // Console.WriteLine("Resolve: " + filePath);
            byte[] fileBytes = File.ReadAllBytes(filePath);
            return HttpHelper.Raw(200, contentType, fileBytes);
        }
    }
}
