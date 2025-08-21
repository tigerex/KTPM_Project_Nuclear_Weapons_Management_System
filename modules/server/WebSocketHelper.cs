using System;
using System.Net.Sockets;
using System.Text;
using System.Security.Cryptography;
using System.Collections.Generic;
using System.Linq;

namespace project_nuclear_weapons_management_system.modules.server
{
    public static class HandleWebSocket
    {
        private static readonly List<TcpClient> _clients = new List<TcpClient>();
        private static readonly object _lock = new object();

        public static void Handle(TcpClient client, NetworkStream stream, Dictionary<string, string> headers)
        {
            try
            {
                // 1. Perform WebSocket handshake
                if (!headers.ContainsKey("Sec-WebSocket-Key"))
                {
                    client.Close();
                    return;
                }

                string key = headers["Sec-WebSocket-Key"];
                string accept = Convert.ToBase64String(
                    SHA1.HashData(Encoding.UTF8.GetBytes(
                        key + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11"
                    ))
                );

                string response = 
                    "HTTP/1.1 101 Switching Protocols\r\n" +
                    "Upgrade: websocket\r\n" +
                    "Connection: Upgrade\r\n" +
                    $"Sec-WebSocket-Accept: {accept}\r\n\r\n";

                byte[] respBytes = Encoding.UTF8.GetBytes(response);
                stream.Write(respBytes, 0, respBytes.Length);

                // 2. Add client to list
                lock (_lock)
                {
                    _clients.Add(client);
                }

                Logger.Log($"[WebSocket] Client joined. Total: {_clients.Count}");

                // 3. Message loop
                while (client.Connected)
                {
                    string msg = ReadWebSocketMessage(stream);
                    if (msg == null)
                        break;

                    Logger.Log($"[WebSocket] Received: {msg}");

                    // broadcast to all clients
                    Broadcast(msg, client);
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"[WebSocket] Error: {ex.Message}");
            }
            finally
            {
                lock (_lock)
                {
                    _clients.Remove(client);
                }
                client.Close();
                Logger.Log($"[WebSocket] Client left. Total: {_clients.Count}");
            }
        }

        private static string? ReadWebSocketMessage(NetworkStream stream)
        {
            int b1 = stream.ReadByte();
            if (b1 == -1) return null;
            int b2 = stream.ReadByte();
            if (b2 == -1) return null;

            bool masked = (b2 & 0b10000000) != 0;
            int payloadLen = b2 & 0b01111111;

            if (payloadLen == 126)
            {
                byte[] ext = new byte[2];
                stream.Read(ext, 0, 2);
                Array.Reverse(ext);
                payloadLen = BitConverter.ToUInt16(ext, 0);
            }
            else if (payloadLen == 127)
            {
                byte[] ext = new byte[8];
                stream.Read(ext, 0, 8);
                Array.Reverse(ext);
                payloadLen = (int)BitConverter.ToUInt64(ext, 0);
            }

            byte[] mask = new byte[4];
            if (masked)
                stream.Read(mask, 0, 4);

            byte[] payload = new byte[payloadLen];
            int read = 0;
            while (read < payloadLen)
            {
                int n = stream.Read(payload, read, payloadLen - read);
                if (n <= 0) break;
                read += n;
            }

            if (masked)
            {
                for (int i = 0; i < payload.Length; i++)
                    payload[i] = (byte)(payload[i] ^ mask[i % 4]);
            }

            return Encoding.UTF8.GetString(payload);
        }

        private static void SendWebSocketMessage(NetworkStream stream, string message)
        {
            byte[] payload = Encoding.UTF8.GetBytes(message);
            List<byte> frame = new List<byte>();
            frame.Add(0x81); // FIN + text frame

            if (payload.Length <= 125)
            {
                frame.Add((byte)payload.Length);
            }
            else if (payload.Length <= ushort.MaxValue)
            {
                frame.Add(126);
                frame.AddRange(BitConverter.GetBytes((ushort)payload.Length).Reverse());
            }
            else
            {
                frame.Add(127);
                frame.AddRange(BitConverter.GetBytes((ulong)payload.Length).Reverse());
            }

            frame.AddRange(payload);

            stream.Write(frame.ToArray(), 0, frame.Count);
        }

        private static void Broadcast(string message, TcpClient sender)
        {
            lock (_lock)
            {
                foreach (var client in _clients.ToList())
                {
                    try
                    {
                        if (client.Connected)
                        {
                            NetworkStream s = client.GetStream();
                            SendWebSocketMessage(s, message);
                        }
                    }
                    catch
                    {
                        client.Close();
                        _clients.Remove(client);
                    }
                }
            }
        }
    }
}
