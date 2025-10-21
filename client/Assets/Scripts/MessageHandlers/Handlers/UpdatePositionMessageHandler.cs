using System;
using System.Threading.Tasks;
using NullZustand;
using UnityEngine;

namespace ClientMessageHandlers.Handlers
{
    public class UpdatePositionMessageHandler : ClientMessageHandler, IClientMessageHandler<Quaternion, float>
    {
        public override string RequestMessageType => MessageTypes.UPDATE_POSITION_REQUEST;
        public override string ResponseMessageType => MessageTypes.UPDATE_POSITION_RESPONSE;

        public async Task<string> SendRequestAsync(ServerController serverController, Quaternion rotation, float velocity, Action<object> onSuccess = null, Action<string> onFailure = null)
        {
            string messageId = GenerateMessageId();
            serverController.RegisterResponseCallbacks(messageId, onSuccess, onFailure);

            await serverController.SendMessageAsync(new Message
            {
                Id = messageId,
                Type = MessageTypes.UPDATE_POSITION_REQUEST,
                Payload = new {
                    rotX = rotation.x,
                    rotY = rotation.y,
                    rotZ = rotation.z,
                    rotW = rotation.w,
                    velocity = velocity
                }
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

