using System.Text;
using System.Text.Json;

namespace project_nuclear_weapons_management_system.modules.server
{
    /// <summary>
    /// Build HTTP/1.1 response thành byte[].
    /// </summary>
    public static class HttpHelper
{
    private static byte[] Build(int status, string contentType, byte[] body, Dictionary<string,string>? extraHeaders=null)
    {
        string reason = status switch { 200=>"OK", 302=>"Found", 401=>"Unauthorized", 404=>"Not Found", 500=>"Internal Server Error", _=>"OK" };
        var sb = new StringBuilder()
            .Append("HTTP/1.1 ").Append(status).Append(' ').Append(reason).Append("\r\n")
            .Append("Content-Type: ").Append(contentType).Append("\r\n")
            .Append("Content-Length: ").Append(body.Length).Append("\r\n")
            .Append("Connection: close\r\n"); // đổi qua keep alive nếu mà log file quá nhiều
        if (extraHeaders != null)
            foreach (var kv in extraHeaders) sb.Append(kv.Key).Append(": ").Append(kv.Value).Append("\r\n");
        sb.Append("\r\n");
        var head = Encoding.UTF8.GetBytes(sb.ToString());
        var resp = new byte[head.Length + body.Length];
        Buffer.BlockCopy(head, 0, resp, 0, head.Length);
        Buffer.BlockCopy(body, 0, resp, head.Length, body.Length);
        return resp;
    }

    public static byte[] Json(int status, object payload, Dictionary<string,string>? extraHeaders=null)
        => Build(status, "application/json", JsonSerializer.SerializeToUtf8Bytes(payload), extraHeaders);

    public static byte[] Raw(int status, string contentType, byte[] body, Dictionary<string,string>? extraHeaders=null)
        => Build(status, contentType, body, extraHeaders);

    public static byte[] Redirect(string location)
        => Build(302, "text/plain; charset=UTF-8", Encoding.UTF8.GetBytes("Found"), new() { ["Location"]=location });
}

}




// using System.Text;
// using System.Text.Json;

// namespace project_nuclear_weapons_management_system.modules.server
// {
//     public static class HttpHelper
//     {
//         /// <summary>
//         /// Build a JSON response with correct headers.
//         /// </summary>
//         public static byte[] Json(int status, object payload)
//         {
//             string reason = GetReason(status);
//             string body = JsonSerializer.Serialize(payload);

//             var header = new StringBuilder();
//             header.Append("HTTP/1.1 ").Append(status).Append(' ').Append(reason).Append("\r\n");
//             header.Append("Content-Type: application/json\r\n");
//             header.Append("Content-Length: ").Append(Encoding.UTF8.GetByteCount(body)).Append("\r\n");
//             header.Append("Connection: keep-alive\r\n\r\n");

//             // Merge headers + body into one byte array
//             byte[] headerBytes = Encoding.UTF8.GetBytes(header.ToString());
//             byte[] bodyBytes = Encoding.UTF8.GetBytes(body);
//             return Combine(headerBytes, bodyBytes);
//         }

//         /// <summary>
//         /// Build a raw response with arbitrary content type and binary body.
//         /// Use this for HTML, CSS, JS, images, etc.
//         /// </summary>
//         public static byte[] Raw(int status, string contentType, byte[] bodyBytes)
//         {
//             string reason = GetReason(status);

//             var header = new StringBuilder();
//             header.Append("HTTP/1.1 ").Append(status).Append(' ').Append(reason).Append("\r\n");
//             header.Append("Content-Type: ").Append(contentType).Append("\r\n");
//             header.Append("Content-Length: ").Append(bodyBytes.Length).Append("\r\n");
//             header.Append("Connection: close\r\n\r\n");

//             byte[] headerBytes = Encoding.UTF8.GetBytes(header.ToString());
//             return Combine(headerBytes, bodyBytes);
//         }

//         // Helper to merge two byte arrays
//         private static byte[] Combine(byte[] first, byte[] second)
//         {
//             byte[] result = new byte[first.Length + second.Length];
//             Buffer.BlockCopy(first, 0, result, 0, first.Length);
//             Buffer.BlockCopy(second, 0, result, first.Length, second.Length);
//             return result;
//         }

//         private static string GetReason(int status) => status switch
//         {
//             200 => "OK",
//             400 => "Bad Request",
//             401 => "Unauthorized",
//             404 => "Not Found",
//             500 => "Internal Server Error",
//             _   => "OK"
//         };
//     }
// }
