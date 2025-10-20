using System;
using System.Threading.Tasks;
using NullZustand;

namespace ClientMessageHandlers.Handlers
{
    public class UpdatePositionHandler : ClientHandler, IClientHandler<float, float, float, float, float, float, float>
    {
        public override string RequestMessageType => MessageTypes.UPDATE_POSITION_REQUEST;
        public override string ResponseMessageType => MessageTypes.UPDATE_POSITION_RESPONSE;

        public async Task<string> SendRequestAsync(ServerController serverController, float x, float y, float z, float rotX, float rotY, float rotZ, float rotW, Action<object> onSuccess = null, Action<string> onFailure = null)
        {
            string messageId = GenerateMessageId();
            serverController.RegisterResponseCallbacks(messageId, onSuccess, onFailure);

            await serverController.SendMessageAsync(new Message
            {
                Id = messageId,
                Type = MessageTypes.UPDATE_POSITION_REQUEST,
                Payload = new { x = x, y = y, z = z, rotX = rotX, rotY = rotY, rotZ = rotZ, rotW = rotW }
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

