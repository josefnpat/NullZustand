using System;
using System.Collections.Generic;
using System.Linq;

namespace NullZustand
{
    public class ChatManager
    {
        private class StoredChatMessage
        {
            public string Username { get; set; }
            public string Message { get; set; }
            public long Timestamp { get; set; }
        }

        private readonly List<StoredChatMessage> _chatHistory;
        private readonly object _lock = new object();
        private const int MAX_CHAT_HISTORY = 100;

        public ChatManager()
        {
            _chatHistory = new List<StoredChatMessage>();
        }

        public void AddMessage(string username, string message)
        {
            lock (_lock)
            {
                var chatMessage = new StoredChatMessage
                {
                    Username = username,
                    Message = message,
                    Timestamp = TimeUtils.GetUnixTimestampMs()
                };

                _chatHistory.Add(chatMessage);

                // Keep only the most recent messages
                if (_chatHistory.Count > MAX_CHAT_HISTORY)
                {
                    _chatHistory.RemoveAt(0);
                }

                Console.WriteLine($"[CHAT] {username}: {message}");
            }
        }

    }
}

