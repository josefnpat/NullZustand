using System;
using System.Collections.Generic;
using System.Net.Sockets;
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

        public async Task<bool> ProcessMessageAsync(string messageType, NetworkStream stream)
        {
            if (_handlers.TryGetValue(messageType, out IMessageHandler handler))
            {
                try
                {
                    Console.WriteLine($"[MESSAGE] Processing: {messageType}");
                    await handler.HandleAsync(stream);
                    return true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ERROR] Handler failed for {messageType}: {ex.Message}");
                    return false;
                }
            }

            Console.WriteLine($"[WARNING] No handler found for message type: {messageType}");
            return false;
        }

        public string[] GetRegisteredMessageTypes()
        {
            return new List<string>(_handlers.Keys).ToArray();
        }
    }
}
