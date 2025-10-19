using System;
using System.Net.Sockets;

namespace NullZustand
{

    public class ClientSession
    {
        public string SessionId { get; }
        public NetworkStream Stream { get; }
        public bool IsAuthenticated { get; set; }
        public string Username { get; set; }
        public string RemoteAddress { get; }

        public ClientSession(TcpClient client)
        {
            SessionId = Guid.NewGuid().ToString("N").Substring(0, 12);
            Stream = client?.GetStream() ?? throw new ArgumentNullException(nameof(client));
            IsAuthenticated = false;
            
            try
            {
                RemoteAddress = client.Client.RemoteEndPoint?.ToString();
            }
            catch
            {
                RemoteAddress = null;
            }
        }

        public void Authenticate(string username)
        {
            Username = username;
            IsAuthenticated = true;
        }

        public override string ToString()
        {
            string auth = IsAuthenticated ? $"Authenticated as {Username}" : "Guest";
            string address = RemoteAddress ?? "Unknown";
            return $"[Session {SessionId}] {address} - {auth}";
        }
    }
}

