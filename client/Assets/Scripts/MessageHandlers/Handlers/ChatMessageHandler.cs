using System;
using System.Threading.Tasks;
using NullZustand;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace ClientMessageHandlers.Handlers
{
    public class ChatMessageHandler : ClientMessageHandler, IClientMessageHandler<string>
    {
        public override string RequestMessageType => MessageTypes.CHAT_MESSAGE_REQUEST;
        public override string ResponseMessageType => MessageTypes.CHAT_MESSAGE_RESPONSE;

        public async Task<string> SendRequestAsync(ServerController serverController, string message, Action<object> onSuccess = null, Action<string> onFailure = null)
        {
            string messageId = GenerateMessageId();
            // Note: We don't register callbacks here because chat messages don't get a direct response
            // Instead, we'll receive the broadcast like everyone else

            await serverController.SendMessageAsync(new Message
            {
                Id = messageId,
                Type = MessageTypes.CHAT_MESSAGE_REQUEST,
                Payload = new { message = message }
            });

            onSuccess?.Invoke(null);

            return messageId;
        }

        public override void HandleResponse(Message message, MessageHandlerContext context)
        {
            JObject payload = GetPayloadAsJObject(message);
            if (payload == null)
            {
                Debug.LogWarning($"[{ResponseMessageType}] Received null or invalid payload");
                return;
            }

            try
            {
                string username = payload["username"]?.Value<string>();
                string chatMessage = payload["message"]?.Value<string>();
                long timestamp = payload["timestamp"]?.Value<long>() ?? 0;

                if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(chatMessage))
                {
                    Debug.LogWarning($"[{ResponseMessageType}] Invalid chat message format");
                    return;
                }

                context.ServerController.InvokeNewChatMessage(username, chatMessage, timestamp);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{ResponseMessageType}] Failed to parse chat message: {ex.Message}");
            }
        }
    }
}

