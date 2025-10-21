using System;
using System.Threading.Tasks;

namespace NullZustand.MessageHandlers.Handlers
{
    public class ChatMessagePayload
    {
        public string message { get; set; }
    }

    public class ChatMessageHandler : MessageHandler
    {
        private readonly ChatManager _chatManager;
        private readonly SessionManager _sessionManager;

        public ChatMessageHandler(ChatManager chatManager, SessionManager sessionManager)
        {
            _chatManager = chatManager ?? throw new ArgumentNullException(nameof(chatManager));
            _sessionManager = sessionManager ?? throw new ArgumentNullException(nameof(sessionManager));
        }

        public override string MessageType => MessageTypes.CHAT_MESSAGE_REQUEST;

        // Requires authentication - only logged in players can send chat messages
        public override bool RequiresAuthentication => true;

        public override async Task HandleAsync(Message message, ClientSession session)
        {
            var payload = GetPayload<ChatMessagePayload>(message);

            if (payload == null || string.IsNullOrWhiteSpace(payload.message))
            {
                Console.WriteLine($"[WARNING] ChatMessage received with null or empty payload from {session.SessionId}");
                await SendAsync(session, new Message
                {
                    Id = message.Id,
                    Type = MessageTypes.ERROR,
                    Payload = new
                    {
                        code = "INVALID_MESSAGE",
                        message = "Chat message cannot be empty."
                    }
                });
                return;
            }

            if (session.Player == null)
            {
                Console.WriteLine($"[WARNING] ChatMessage received but session has no player: {session.SessionId}");
                return;
            }

            // Validate message length
            if (payload.message.Length > 500)
            {
                Console.WriteLine($"[WARNING] Chat message too long from {session.Username}: {payload.message.Length} characters");
                await SendAsync(session, new Message
                {
                    Id = message.Id,
                    Type = MessageTypes.ERROR,
                    Payload = new
                    {
                        code = "MESSAGE_TOO_LONG",
                        message = "Chat message cannot exceed 500 characters."
                    }
                });
                return;
            }

            // Add message to chat history
            _chatManager.AddMessage(session.Username, payload.message);

            // Broadcast to all authenticated sessions
            await BroadcastChatMessageAsync(session.Username, payload.message);
        }

        private async Task BroadcastChatMessageAsync(string username, string message)
        {
            var sessions = _sessionManager.GetAllAuthenticatedSessions();
            long timestamp = TimeUtils.GetUnixTimestampMs();
            
            foreach (var session in sessions)
            {
                try
                {
                    await SendAsync(session, new Message
                    {
                        Id = Guid.NewGuid().ToString(),
                        Type = MessageTypes.CHAT_MESSAGE_RESPONSE,
                        Payload = new { username = username, message = message, timestamp = timestamp }
                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ERROR] Failed to broadcast chat message to {session.SessionId}: {ex.Message}");
                }
            }
        }
    }
}

