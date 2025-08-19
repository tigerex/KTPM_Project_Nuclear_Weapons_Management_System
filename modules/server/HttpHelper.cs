using System.Text;
using System.Text.Json;

namespace project_nuclear_weapons_management_system.modules.server
{
    public static class HttpHelper
    {
        public static string Json(int status, object payload)
        {
            string reason = status switch {
                200 => "OK", 400 => "Bad Request", 401 => "Unauthorized",
                404 => "Not Found", 500 => "Internal Server Error", _ => "OK"
            };
            var body = JsonSerializer.Serialize(payload);
            var sb = new StringBuilder();
            sb.Append("HTTP/1.1 ").Append(status).Append(' ').Append(reason).Append("\r\n");
            sb.Append("Content-Type: application/json\r\n");
            sb.Append("Content-Length: ").Append(Encoding.UTF8.GetByteCount(body)).Append("\r\n");
            sb.Append("Connection: keep-alive\r\n\r\n").Append(body);
            return sb.ToString();
        }
    }
}
