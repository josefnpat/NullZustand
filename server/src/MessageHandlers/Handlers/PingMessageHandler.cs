using System;
using System.Threading.Tasks;

namespace NullZustand.MessageHandlers.Handlers
{
    public class PingMessageHandler : MessageHandler
    {
        public override string MessageType => MessageTypes.PING;

        public override async Task HandleAsync(Message message, ClientSession session)
        {
            Message response = new Message
            {
                Type = MessageTypes.PONG,
                Payload = new { time = DateTime.UtcNow }
            };
            await SendAsync(session, response);
        }
    }
}
