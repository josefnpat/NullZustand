using System;
using System.IO;
using System.Net.Sockets;

namespace NullZustand
{

    public class ClientSession
    {
        public string SessionId { get; }
        public Stream Stream { get; }
        public bool IsAuthenticated { get; set; }
        public string Username { get; set; }
        public Player Player { get; set; }
        public string RemoteAddress { get; }
        public DateTime LastActivityTime { get; private set; }

        public ClientSession(TcpClient client, Stream stream)
        {
            SessionId = Guid.NewGuid().ToString("N").Substring(0, 12);
            Stream = stream ?? throw new ArgumentNullException(nameof(stream));
            IsAuthenticated = false;
            LastActivityTime = DateTime.UtcNow;

            try
            {
                RemoteAddress = client?.Client?.RemoteEndPoint?.ToString();
            }
            catch
            {
                RemoteAddress = null;
            }
        }

        public void Authenticate(string username, Player player)
        {
            Username = username;
            Player = player ?? throw new ArgumentNullException(nameof(player));
            IsAuthenticated = true;
            UpdateActivity();
        }

        public void UpdateActivity()
        {
            LastActivityTime = DateTime.UtcNow;
        }

        public TimeSpan GetIdleTime()
        {
            return DateTime.UtcNow - LastActivityTime;
        }

        public override string ToString()
        {
            string auth = IsAuthenticated ? $"Authenticated as {Username}" : "Guest";
            string address = RemoteAddress ?? "Unknown";
            return $"[Session {SessionId}] {address} - {auth}";
        }
    }
}

