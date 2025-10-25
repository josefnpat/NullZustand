using System;
using System.Threading.Tasks;
using NullZustand;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace ClientMessageHandlers.Handlers
{
    public class RegisterMessageHandler : ClientMessageHandler, IClientMessageHandler<RegisterRequest>
    {
        public override string RequestMessageType => MessageTypes.REGISTER_REQUEST;
        public override string ResponseMessageType => MessageTypes.REGISTER_RESPONSE;

        public async Task<string> SendRequestAsync(ServerController serverController, RegisterRequest request, Action<object> onSuccess = null, Action<string> onFailure = null)
        {
            string messageId = GenerateMessageId();
            serverController.RegisterResponseCallbacks(messageId, onSuccess, onFailure);

            await serverController.SendMessageAsync(new Message
            {
                Id = messageId,
                Type = MessageTypes.REGISTER_REQUEST,
                Payload = new { username = request.Username, password = request.Password }
            });

            return messageId;
        }

        public override void HandleResponse(Message message, MessageHandlerContext context)
        {
            JObject payload = GetPayloadAsJObject(message);
            if (payload == null)
            {
                Debug.LogWarning($"[{ResponseMessageType}] Received null or invalid payload");
                if (message.Id != null)
                {
                    context.ServerController.InvokeResponseFailure(message.Id, "Invalid payload");
                }
                return;
            }

            bool success = payload["success"]?.Value<bool>() ?? false;
            if (success)
            {
                context.ServerController.InvokeResponseSuccess(message.Id, payload);
            }
            else
            {
                string error = payload["error"]?.Value<string>() ?? "Unknown error";
                context.ServerController.InvokeResponseFailure(message.Id, error);
            }
        }
    }

    [Serializable]
    public class RegisterRequest
    {
        public string Username { get; set; }
        public string Password { get; set; }

        public RegisterRequest(string username, string password)
        {
            Username = username;
            Password = password;
        }
    }
}

