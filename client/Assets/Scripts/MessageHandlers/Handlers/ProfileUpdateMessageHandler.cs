using System;
using System.Threading.Tasks;
using NullZustand;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace ClientMessageHandlers.Handlers
{
    public class ProfileUpdateMessageHandler : ClientMessageHandler, IClientMessageHandler<ProfileUpdateRequest>
    {
        public override string RequestMessageType => MessageTypes.PROFILE_UPDATE_REQUEST;
        public override string ResponseMessageType => MessageTypes.PROFILE_UPDATE_RESPONSE;
        public override string BroadcastMessageType => MessageTypes.PROFILE_UPDATE_BROADCAST;

        public async Task<string> SendRequestAsync(ServerController serverController, ProfileUpdateRequest request, Action<object> onSuccess = null, Action<string> onFailure = null)
        {
            string messageId = GenerateMessageId();
            serverController.RegisterResponseCallbacks(messageId, onSuccess, onFailure);

            await serverController.SendMessageAsync(new Message
            {
                Id = messageId,
                Type = MessageTypes.PROFILE_UPDATE_REQUEST,
                Payload = new {
                    profileImage = request.ProfileImage
                }
            });

            return messageId;
        }

        public override void HandleResponse(Message message, MessageHandlerContext context)
        {
            JObject payload = GetPayloadAsJObject(message);
            if (payload == null)
            {
                Debug.LogWarning($"[{message.Type}] Received null or invalid payload");
                if (message.Id != null)
                    context.ServerController.InvokeResponseFailure(message.Id, "Invalid payload");
                return;
            }

            if (message.Type == MessageTypes.PROFILE_UPDATE_BROADCAST)
            {
                HandleProfileUpdateBroadcast(payload, context);
            }
            else if (message.Type == MessageTypes.PROFILE_UPDATE_RESPONSE)
            {
                HandleProfileUpdateResponse(payload, message.Id, context);
            }
        }

        private void HandleProfileUpdateResponse(JObject payload, string messageId, MessageHandlerContext context)
        {
            try
            {
                bool success = payload["success"]?.Value<bool>() ?? false;

                if (success)
                {
                    Debug.Log($"[ProfileUpdate] Profile update successful");
                    context.ServerController.InvokeResponseSuccess(messageId, payload);
                }
                else
                {
                    string error = payload["error"]?.Value<string>() ?? "Unknown error";
                    Debug.LogWarning($"[ProfileUpdate] Profile update failed: {error}");
                    context.ServerController.InvokeResponseFailure(messageId, error);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ProfileUpdate] Failed to parse response: {ex.Message}");
                context.ServerController.InvokeResponseFailure(messageId, "Failed to parse response");
            }
        }

        private void HandleProfileUpdateBroadcast(JObject payload, MessageHandlerContext context)
        {
            try
            {
                string username = payload["username"]?.Value<string>();
                int profileImage = payload["profileImage"]?.Value<int>() ?? -1;

                if (string.IsNullOrEmpty(username))
                {
                    Debug.LogWarning($"[ProfileUpdate] Invalid broadcast format");
                    return;
                }

                Debug.Log($"[ProfileUpdate] Player {username} updated profile: profileImage={profileImage}");

                // Trigger profile update event for UI or other systems
                context.ServerController.InvokeNewProfileUpdate(username, profileImage);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ProfileUpdate] Failed to parse broadcast: {ex.Message}");
            }
        }
    }

    [Serializable]
    public class ProfileUpdateRequest
    {
        public int ProfileImage { get; set; }

        public ProfileUpdateRequest(int profileImage)
        {
            ProfileImage = profileImage;
        }
    }
}
