using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.Sockets;

namespace NullZustand
{
    public class SessionManager
    {
        private readonly ConcurrentDictionary<string, ClientSession> _sessions;
        private readonly PlayerManager _playerManager;

        public SessionManager(PlayerManager playerManager)
        {
            _sessions = new ConcurrentDictionary<string, ClientSession>();
            _playerManager = playerManager ?? throw new ArgumentNullException(nameof(playerManager));
        }

        public ClientSession RegisterSession(TcpClient client, Stream stream)
        {
            var session = new ClientSession(client, stream);
            if (_sessions.TryAdd(session.SessionId, session))
            {
                Console.WriteLine($"[SESSION] Registered: {session}");
                Console.WriteLine($"[SESSION] Total active sessions: {_sessions.Count}");
                return session;
            }

            throw new InvalidOperationException("Failed to register session");
        }

        public void AuthenticateSession(string sessionId, string username)
        {
            if (_sessions.TryGetValue(sessionId, out ClientSession session))
            {
                // Get or create the persistent player object
                Player player = _playerManager.GetOrCreatePlayer(username);
                session.Authenticate(username, player);
                Console.WriteLine($"[SESSION] Authenticated: {session}");
            }
        }

        public void RemoveSession(string sessionId)
        {
            if (_sessions.TryRemove(sessionId, out ClientSession session))
            {
                Console.WriteLine($"[SESSION] Removed: {session}");
                Console.WriteLine($"[SESSION] Total active sessions: {_sessions.Count}");
            }
        }
    }
}

