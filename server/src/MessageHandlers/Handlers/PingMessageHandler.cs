using System;
using System.Threading.Tasks;

namespace NullZustand.MessageHandlers.Handlers
{
    public class PingMessageHandler : MessageHandler
    {
        public override string MessageType => MessageTypes.PING;

        public override async Task HandleAsync(Message message, ClientSession session)
        {
            await SendResponseAsync(session, message, MessageTypes.PONG,
                new { time = DateTime.UtcNow });
        }
    }
}
