using System;
using System.Threading.Tasks;
using NullZustand;

namespace ClientMessageHandlers.Handlers
{
    public class PingHandler : ClientHandler, IClientHandlerNoParam
    {
        public override string RequestMessageType => MessageTypes.PING;
        public override string ResponseMessageType => MessageTypes.PONG;

        public async Task<string> SendRequestAsync(ServerController serverController, Action<object> onSuccess = null, Action<string> onFailure = null)
        {
            string messageId = GenerateMessageId();
            serverController.RegisterResponseCallbacks(messageId, onSuccess, onFailure);

            await serverController.SendMessageAsync(new Message
            {
                Id = messageId,
                Type = MessageTypes.PING,
                Payload = new { }
            });

            return messageId;
        }

        public override void HandleResponse(Message message, ServerController serverController)
        {
            var payload = GetPayloadAsJObject(message);
            serverController.InvokeResponseSuccess(message.Id, payload);
        }
    }
}

