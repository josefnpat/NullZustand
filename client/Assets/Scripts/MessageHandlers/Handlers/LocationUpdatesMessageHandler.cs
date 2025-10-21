using System;
using System.Threading.Tasks;
using NullZustand;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace ClientMessageHandlers.Handlers
{
    public class LocationUpdatesMessageHandler : ClientMessageHandler, IClientMessageHandlerNoParam
    {
        public override string RequestMessageType => MessageTypes.LOCATION_UPDATES_REQUEST;
        public override string ResponseMessageType => MessageTypes.LOCATION_UPDATES_RESPONSE;

        public async Task<string> SendRequestAsync(ServerController serverController, Action<object> onSuccess = null, Action<string> onFailure = null)
        {
            string messageId = GenerateMessageId();
            serverController.RegisterResponseCallbacks(messageId, onSuccess, onFailure);

            long lastUpdateId = serverController.GetLastLocationUpdateId();
            await serverController.SendMessageAsync(new Message
            {
                Id = messageId,
                Type = MessageTypes.LOCATION_UPDATES_REQUEST,
                Payload = new { lastUpdateId = lastUpdateId }
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

            // Update lastLocationUpdateId
            if (payload["lastLocationUpdateId"] != null)
            {
                long updateId = payload["lastLocationUpdateId"].Value<long>();
                serverController.SetLastLocationUpdateId(updateId);
            }

            // Apply incremental updates
            if (payload["updates"] != null)
            {
                var updates = payload["updates"] as JArray;
                foreach (var update in updates)
                {
                    string username = update["username"].Value<string>();
                    float x = update["x"]?.Value<float>() ?? 0f;
                    float y = update["y"]?.Value<float>() ?? 0f;
                    float z = update["z"]?.Value<float>() ?? 0f;
                    float rotX = update["rotX"]?.Value<float>() ?? 0f;
                    float rotY = update["rotY"]?.Value<float>() ?? 0f;
                    float rotZ = update["rotZ"]?.Value<float>() ?? 0f;
                    float rotW = update["rotW"]?.Value<float>() ?? 1f;
                    float velocity = update["velocity"]?.Value<float>() ?? 0f;
                    long timestampMs = update["timestampMs"]?.Value<long>() ?? 0L;

                    var position = new Vector3(x, y, z);
                    var rotation = new Quaternion(rotX, rotY, rotZ, rotW);
                    serverController.UpdatePlayerLocation(username, position, rotation, velocity, timestampMs);
                }
            }

            serverController.InvokeResponseSuccess(message.Id, payload);
        }
    }
}

