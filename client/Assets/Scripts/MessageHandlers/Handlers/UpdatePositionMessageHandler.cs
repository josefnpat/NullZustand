using System;
using System.Threading.Tasks;
using NullZustand;
using UnityEngine;

namespace ClientMessageHandlers.Handlers
{
    public class UpdatePositionMessageHandler : ClientMessageHandler, IClientMessageHandler<UpdatePositionRequest>
    {
        public override string RequestMessageType => MessageTypes.UPDATE_POSITION_REQUEST;
        public override string ResponseMessageType => MessageTypes.UPDATE_POSITION_RESPONSE;

        public async Task<string> SendRequestAsync(ServerController serverController, UpdatePositionRequest request, Action<object> onSuccess = null, Action<string> onFailure = null)
        {
            string messageId = GenerateMessageId();
            serverController.RegisterResponseCallbacks(messageId, onSuccess, onFailure);

            await serverController.SendMessageAsync(new Message
            {
                Id = messageId,
                Type = MessageTypes.UPDATE_POSITION_REQUEST,
                Payload = new {
                    rotX = request.Rotation.x,
                    rotY = request.Rotation.y,
                    rotZ = request.Rotation.z,
                    rotW = request.Rotation.w,
                    velocity = request.Velocity
                }
            });

            return messageId;
        }

        public override void HandleResponse(Message message, MessageHandlerContext context)
        {
            var payload = GetPayloadAsJObject(message);
            context.ServerController.InvokeResponseSuccess(message.Id, payload);
        }
    }

    [Serializable]
    public class UpdatePositionRequest
    {
        public Quaternion Rotation { get; set; }
        public float Velocity { get; set; }

        public UpdatePositionRequest(Quaternion rotation, float velocity)
        {
            Rotation = rotation;
            Velocity = velocity;
        }
    }
}

