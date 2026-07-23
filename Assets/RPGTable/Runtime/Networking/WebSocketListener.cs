using System;
using System.IO;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace RPGTable.Runtime.Networking
{
    public static class WebSocketListener
    {
        public static bool PerformHandshake(NetworkStream stream, string headerStr)
        {
            try
            {
                var keyLine = "";
                foreach (var line in headerStr.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None))
                {
                    if (line.StartsWith("Sec-WebSocket-Key:", StringComparison.OrdinalIgnoreCase))
                    {
                        keyLine = line.Substring("Sec-WebSocket-Key:".Length).Trim();
                        break;
                    }
                }

                if (string.IsNullOrEmpty(keyLine)) return false;

                var acceptKey = Convert.ToBase64String(
                    SHA1.Create().ComputeHash(
                        Encoding.UTF8.GetBytes(keyLine + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11")
                    )
                );

                var response = "HTTP/1.1 101 Switching Protocols\r\n" +
                               "Upgrade: websocket\r\n" +
                               "Connection: Upgrade\r\n" +
                               "Sec-WebSocket-Accept: " + acceptKey + "\r\n\r\n";

                var bytes = Encoding.UTF8.GetBytes(response);
                stream.Write(bytes, 0, bytes.Length);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[WebSocketListener] Handshake failed: {ex.Message}");
                return false;
            }
        }

        public static void SendWebSocketFrame(NetworkStream stream, string message)
        {
            try
            {
                var payload = Encoding.UTF8.GetBytes(message);
                var frame = new MemoryStream();
                frame.WriteByte(0x81); // Text frame final

                if (payload.Length <= 125)
                {
                    frame.WriteByte((byte)payload.Length);
                }
                else if (payload.Length <= 65535)
                {
                    frame.WriteByte(126);
                    frame.WriteByte((byte)((payload.Length >> 8) & 0xFF));
                    frame.WriteByte((byte)(payload.Length & 0xFF));
                }
                else
                {
                    frame.WriteByte(127);
                    for (int i = 7; i >= 0; i--)
                    {
                        frame.WriteByte((byte)((payload.Length >> (i * 8)) & 0xFF));
                    }
                }

                frame.Write(payload, 0, payload.Length);
                var frameBytes = frame.ToArray();
                stream.Write(frameBytes, 0, frameBytes.Length);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[WebSocketListener] Send frame failed: {ex.Message}");
            }
        }
    }
}
