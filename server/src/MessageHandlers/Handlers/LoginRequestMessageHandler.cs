using System.Net.Sockets;
using System.Threading.Tasks;

namespace NullZustand.MessageHandlers.Handlers
{
    public class LoginRequestMessageHandler : MessageHandler
    {
        public override string MessageType => MessageTypes.LOGIN_REQUEST;

        public override async Task HandleAsync(NetworkStream stream)
        {
            Message response = new Message
            {
                Type = MessageTypes.LOGIN_RESPONSE,
                Payload = new { success = true, sessionToken = "abc123" }
            };
            await SendAsync(stream, response);
        }
    }
}
