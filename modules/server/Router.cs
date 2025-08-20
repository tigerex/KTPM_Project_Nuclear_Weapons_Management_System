using System.Text;
using project_nuclear_weapons_management_system.modules.database;

namespace project_nuclear_weapons_management_system.modules.server
{
    public static class Router
    {
        // Single entrypoint for server
        /// <summary>
        /// Single entrypoint cho server.
        /// path bắt đầu bằng /api/ → gọi handler; ngược lại → static files.
        /// </summary>
        public static byte[] Resolve(string path, string body, IUserRepository repo, Dictionary<string,string> headers)
        {
            if (path.StartsWith("/api/"))
            {
                var handler = HandlerFactory.Create(path, repo);
                return handler.Handle(body, headers);
            }
            else
            {
                return HandleStatic(path, headers); // truyền headers vào static
            }
        }
        

        // helper parse cookie
        // private static string? GetCookie(Dictionary<string,string> headers, string name)
        // {
        //     if (!headers.TryGetValue("Cookie", out var cookie) || string.IsNullOrEmpty(cookie)) return null;
        //     foreach (var part in cookie.Split(';'))
        //     {
        //         var kv = part.Split('=', 2, StringSplitOptions.TrimEntries);
        //         if (kv.Length == 2 && kv[0] == name) return kv[1];
        //     }
        //     return null;
        // }

        // private static bool IsProtectedPath(string path)
        // {
        //     path = (path ?? "").ToLowerInvariant();
        //     return path.StartsWith("/home") 
        //         || path.StartsWith("/admin")
        //         || path.StartsWith("/profile"); // 👈 thêm dòng này
        // }


        // --- Static file serving ---
        private static byte[] HandleStatic(string path, Dictionary<string, string> headers)
        {

            // Nếu vào trang "protected" mà CHƯA đăng nhập → ép về /login
            // if (IsProtectedPath(path))
            // {
            //     var token = GetCookie(headers, "authToken");
            //     bool ok = !string.IsNullOrEmpty(token) && AuthService.Instance.Validate($"Bearer {token}") != null;
            //     if (!ok)
            //         return HttpHelper.Redirect("/login");
            //     else
            //         return HttpHelper.Redirect("/home"); // nếu đã đăng nhập thì cho vào trang yêu cầu
            // }

            // ======= Phần resolve file như cũ =======
            if (path == "/" || string.IsNullOrEmpty(path))
                path = "/default";

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

            byte[] fileBytes = File.ReadAllBytes(filePath);
            return HttpHelper.Raw(200, contentType, fileBytes);
        }

        

    }
}
