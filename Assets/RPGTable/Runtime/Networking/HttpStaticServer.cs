using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

namespace RPGTable.Runtime.Networking
{
    public static class HttpStaticServer
    {
        public static void ServeStaticFile(string url, NetworkStream stream, string cachedStreamingAssetsPath)
        {
            if (url == "/") url = "/index.html";
            
            // Protection against path traversal
            if (url.Contains("..")) 
            {
                SendResponse(stream, 400, "Bad Request", "text/plain", Encoding.UTF8.GetBytes("Bad Request"));
                return;
            }

            int queryIndex = url.IndexOf('?');
            if (queryIndex != -1) url = url.Substring(0, queryIndex);

            string filePath = Path.Combine(cachedStreamingAssetsPath, "WebClient", url.TrimStart('/'));

            if (File.Exists(filePath))
            {
                byte[] content = File.ReadAllBytes(filePath);
                string mimeType = GetMimeType(filePath);
                SendResponse(stream, 200, "OK", mimeType, content);
            }
            else
            {
                SendResponse(stream, 404, "Not Found", "text/plain", Encoding.UTF8.GetBytes("404 Not Found"));
            }
        }

        public static void SendResponse(NetworkStream stream, int statusCode, string statusText, string mimeType, byte[] contentBytes)
        {
            try
            {
                string header = $"HTTP/1.1 {statusCode} {statusText}\r\n" +
                                $"Content-Type: {mimeType}; charset=utf-8\r\n" +
                                $"Content-Length: {contentBytes.Length}\r\n" +
                                "Access-Control-Allow-Origin: *\r\n" +
                                "Connection: close\r\n\r\n";
                                
                byte[] headerBytes = Encoding.UTF8.GetBytes(header);
                stream.Write(headerBytes, 0, headerBytes.Length);
                if (contentBytes != null && contentBytes.Length > 0)
                {
                    stream.Write(contentBytes, 0, contentBytes.Length);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[HttpStaticServer] SendResponse failed: {ex.Message}");
            }
        }

        public static string GetMimeType(string filePath)
        {
            string ext = Path.GetExtension(filePath).ToLowerInvariant();
            switch (ext)
            {
                case ".html": return "text/html";
                case ".css": return "text/css";
                case ".js": return "application/javascript";
                case ".json": return "application/json";
                case ".png": return "image/png";
                case ".jpg": case ".jpeg": return "image/jpeg";
                default: return "application/octet-stream";
            }
        }
    }
}
