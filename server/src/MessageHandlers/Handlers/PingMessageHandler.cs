using System;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace NullZustand.MessageHandlers.Handlers
{
    public class PingMessageHandler : MessageHandler
    {
        public override string MessageType => MessageTypes.PING;

        public override async Task HandleAsync(Message message, NetworkStream stream)
        {
            Message response = new Message
            {
                Type = MessageTypes.PONG,
                Payload = new { time = DateTime.UtcNow }
            };
            await SendAsync(stream, response);
        }
    }
}
