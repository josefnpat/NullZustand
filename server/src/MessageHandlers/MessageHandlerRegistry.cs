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
            Console.WriteLine($"[HANDLER] Registered: {handler.MessageType}");
        }

        public async Task<bool> ProcessMessageAsync(Message message, ClientSession session)
        {
            if (_handlers.TryGetValue(message.Type, out IMessageHandler handler))
            {
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
    }
}
