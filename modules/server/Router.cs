using System.Text;
using project_nuclear_weapons_management_system.modules.database;

namespace project_nuclear_weapons_management_system.modules.server
{
    public static class Router
    {
        // Single entrypoint for server
        public static byte[] Resolve(string path, string body, IUserRepository repo)
        {
            if (path.StartsWith("/api/"))
            {
                var handler = HandlerFactory.Create(path, repo);
                return handler.Handle(body);
            }
            else
            {
                return HandleStatic(path);
            }
        }

        // --- Static file serving ---
        private static byte[] HandleStatic(string path)
        {
            if (path == "/" || string.IsNullOrEmpty(path))
                path = "/default"; // default page

            path = path.Replace("..", ""); // prevent directory traversal
            // Console.WriteLine("Request path: " + path);

            string filePath;

            // --- Case 0: direct /data/... route ---
            if (path.StartsWith("/data/"))
            {
                filePath = Path.Combine(path.TrimStart('/')); 
                // → stays in data/ folder, not under pages/
            }
            // --- Case 1: folder-style route (e.g. /login → pages/login/login.html) ---
            else if (!Path.HasExtension(path))
            {
                string folder = path.TrimStart('/');
                filePath = Path.Combine("pages", folder, folder + ".html");
            }
            else
            {
                string ext = Path.GetExtension(path).ToLowerInvariant();

                // --- Case 2: special handling for "root-level" CSS/JS of a section ---
                if ((ext == ".css" || ext == ".js") && path.Count(c => c == '/') == 1)
                {
                    string name = Path.GetFileNameWithoutExtension(path);
                    filePath = Path.Combine("pages", name, Path.GetFileName(path));
                }
                else
                {
                    // --- Case 3: regular assets inside /pages/... ---
                    filePath = Path.Combine("pages", path.TrimStart('/'));
                }
            }

            // Console.WriteLine("Resolved file: " + filePath);
            // If requested file doesn't exist → fallback to default.html
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
                ".css"  => "text/css",
                ".js"   => "application/javascript",
                ".png"  => "image/png",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".gif"  => "image/gif",
                ".svg"  => "image/svg+xml",
                _       => "application/octet-stream"
            };

            byte[] fileBytes = File.ReadAllBytes(filePath);
            return HttpHelper.Raw(200, contentType, fileBytes);
        }
    }
}
