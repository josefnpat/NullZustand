using System;
using System.Threading.Tasks;
using NullZustand;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace ClientMessageHandlers.Handlers
{
    public class RegisterHandler : ClientHandler, IClientHandler<string, string>
    {
        public override string RequestMessageType => MessageTypes.REGISTER_REQUEST;
        public override string ResponseMessageType => MessageTypes.REGISTER_RESPONSE;

        public async Task<string> SendRequestAsync(ServerController serverController, string username, string password, Action<object> onSuccess = null, Action<string> onFailure = null)
        {
            string messageId = GenerateMessageId();
            serverController.RegisterResponseCallbacks(messageId, onSuccess, onFailure);

            await serverController.SendMessageAsync(new Message
            {
                Id = messageId,
                Type = MessageTypes.REGISTER_REQUEST,
                Payload = new { username = username, password = password }
            });

            return messageId;
        }

        public override void HandleResponse(Message message, ServerController serverController)
        {
            JObject payload = GetPayloadAsJObject(message);
            if (payload == null)
            {
                Debug.LogWarning($"[{ResponseMessageType}] Received null or invalid payload");
                if (message.Id != null)
                    serverController.InvokeResponseFailure(message.Id, "Invalid payload");
                return;
            }

            bool success = payload["success"]?.Value<bool>() ?? false;
            if (success)
            {
                serverController.InvokeResponseSuccess(message.Id, payload);
            }
            else
            {
                string error = payload["error"]?.Value<string>() ?? "Unknown error";
                serverController.InvokeResponseFailure(message.Id, error);
            }
        }
    }
}

