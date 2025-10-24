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
        public override string BroadcastMessageType => MessageTypes.LOCATION_UPDATES_BROADCAST;

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

            if (message.Type == MessageTypes.LOCATION_UPDATES_BROADCAST)
            {
                ProcessSingleUpdate(payload, context);
            }
            else if (message.Type == MessageTypes.LOCATION_UPDATES_RESPONSE)
            {
                // Update lastLocationUpdateId
                if (payload["lastLocationUpdateId"] != null)
                {
                    long updateId = payload["lastLocationUpdateId"].Value<long>();
                    context.ServerController.SetLastLocationUpdateId(updateId);
                }

                // Apply incremental updates
                if (payload["updates"] != null)
                {
                    var updates = payload["updates"] as JArray;
                    foreach (var update in updates)
                    {
                        ProcessSingleUpdate(update, context);
                    }
                }

                context.ServerController.InvokeResponseSuccess(message.Id, payload);
            }
        }

        private void ProcessSingleUpdate(JToken update, MessageHandlerContext context)
        {
            try
            {
                string username = update["username"]?.Value<string>();
                if (string.IsNullOrEmpty(username))
                {
                    Debug.LogWarning("[LocationUpdate] Invalid update: missing username");
                    return;
                }

                long updateId = update["updateId"]?.Value<long>() ?? 0;
                float x = update["x"]?.Value<float>() ?? 0f;
                float y = update["y"]?.Value<float>() ?? 0f;
                float z = update["z"]?.Value<float>() ?? 0f;
                float rotX = update["rotX"]?.Value<float>() ?? 0f;
                float rotY = update["rotY"]?.Value<float>() ?? 0f;
                float rotZ = update["rotZ"]?.Value<float>() ?? 0f;
                float rotW = update["rotW"]?.Value<float>() ?? 1f;
                float velocity = update["velocity"]?.Value<float>() ?? 0f;
                long timestampMs = update["timestampMs"]?.Value<long>() ?? 0L;

                // Update the last location update ID (for sync purposes)
                if (updateId > context.ServerController.GetLastLocationUpdateId())
                {
                    context.ServerController.SetLastLocationUpdateId(updateId);
                }

                // Convert to Unity types
                var position = new Vector3(x, y, z);
                var rotation = new Quaternion(rotX, rotY, rotZ, rotW);
                var player = new Player(username);

                long entityId = update["entityId"]?.Value<long>() ?? EntityManager.INVALID_ENTITY_ID;
                if (entityId != EntityManager.INVALID_ENTITY_ID)
                {
                    player.EntityId = entityId;
                    context.EntityManager.CreateEntity(entityId, position, rotation, velocity, timestampMs);
                }

                context.ServerController.TriggerPlayerUpdate(player);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LocationUpdate] Failed to process update: {ex.Message}");
            }
        }
    }
}

