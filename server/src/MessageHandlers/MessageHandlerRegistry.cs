using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NullZustand.MessageHandlers
{
    public class MessageHandlerRegistry
    {
        private readonly Dictionary<string, IMessageHandler> _handlers = new Dictionary<string, IMessageHandler>();

        public void RegisterHandler(IMessageHandler handler)
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            _handlers[handler.MessageType] = handler;
            string authStatus = handler.RequiresAuthentication ? "requires auth" : "no auth required";
            Console.WriteLine($"[HANDLER] Registered: {handler.MessageType} ({authStatus})");
        }

        public async Task<bool> ProcessMessageAsync(Message message, ClientSession session)
        {
            if (_handlers.TryGetValue(message.Type, out IMessageHandler handler))
            {
                if (handler.RequiresAuthentication && !session.IsAuthenticated)
                {
                    Console.WriteLine($"[AUTH] Rejected {message.Type} from unauthenticated session {session.SessionId}");
                    await SendAuthenticationRequiredAsync(session, message.Type);
                    return false;
                }

                try
                {
                    Console.WriteLine($"[MESSAGE] Processing {message.Type} for session {session.SessionId}");
                    await handler.HandleAsync(message, session);
                    return true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ERROR] Handler failed for {message.Type}: {ex.Message}");
                    return false;
                }
            }

            Console.WriteLine($"[WARNING] No handler found for message type: {message.Type}");
            return false;
        }

        private async Task SendAuthenticationRequiredAsync(ClientSession session, string messageType)
        {
            try
            {
                var errorMessage = new Message
                {
                    Type = MessageTypes.ERROR,
                    Payload = new
                    {
                        code = "AUTHENTICATION_REQUIRED",
                        message = $"Message type '{messageType}' requires authentication",
                        originalMessageType = messageType
                    }
                };

                string json = Newtonsoft.Json.JsonConvert.SerializeObject(errorMessage);
                await MessageFraming.WriteMessageAsync(session.Stream, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to send authentication error: {ex.Message}");
            }
        }
    }
}
