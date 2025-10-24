using System;
using NullZustand;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace ClientMessageHandlers.Handlers
{
    public class ResyncRequiredMessageHandler : ClientMessageHandler, IClientMessageHandlerNoParam
    {
        public override string RequestMessageType => null; // Resync is only received, never sent
        public override string ResponseMessageType => MessageTypes.RESYNC_REQUIRED_RESPONSE;

        public System.Threading.Tasks.Task<string> SendRequestAsync(ServerController serverController, Action<object> onSuccess = null, Action<string> onFailure = null)
        {
            // Resync is only received, never sent by client
            return System.Threading.Tasks.Task.FromResult<string>(null);
        }

        public override void HandleResponse(Message message, MessageHandlerContext context)
        {
            JObject payload = GetPayloadAsJObject(message);
            if (payload == null)
            {
                Debug.LogWarning($"[{ResponseMessageType}] Received null or invalid payload");
                return;
            }

            Debug.LogWarning($"[ResyncHandler] Resync required - applying full location data");

            // Update lastLocationUpdateId
            if (payload["lastLocationUpdateId"] != null)
            {
                long updateId = payload["lastLocationUpdateId"].Value<long>();
                context.ServerController.SetLastLocationUpdateId(updateId);
            }

            // Load all entity states
            if (payload["allEntities"] != null)
            {
                var allEntities = payload["allEntities"] as JArray;
                foreach (var entity in allEntities)
                {
                    float x = entity["x"]?.Value<float>() ?? 0f;
                    float y = entity["y"]?.Value<float>() ?? 0f;
                    float z = entity["z"]?.Value<float>() ?? 0f;
                    float rotX = entity["rotX"]?.Value<float>() ?? 0f;
                    float rotY = entity["rotY"]?.Value<float>() ?? 0f;
                    float rotZ = entity["rotZ"]?.Value<float>() ?? 0f;
                    float rotW = entity["rotW"]?.Value<float>() ?? 1f;
                    float velocity = entity["velocity"]?.Value<float>() ?? 0f;
                    long timestampMs = entity["timestampMs"]?.Value<long>() ?? 0L;
                    string entityTypeStr = entity["entityType"]?.Value<string>() ?? "Player";

                    // Parse entity type
                    EntityType entityType = NullZustand.EntityTypeUtils.ParseEntityType(entityTypeStr);

                    var position = new Vector3(x, y, z);
                    var rotation = new Quaternion(rotX, rotY, rotZ, rotW);
                    long entityId = entity["entityId"]?.Value<long>() ?? EntityManager.INVALID_ENTITY_ID;

                    if (entityId != EntityManager.INVALID_ENTITY_ID)
                    {
                        if (context.EntityManager.HasEntity(entityId))
                        {
                            context.EntityManager.UpdateEntity(entityId, position, rotation, velocity, timestampMs);
                        }
                        else
                        {
                            context.EntityManager.CreateEntity(entityId, entityType, position, rotation, velocity, timestampMs);
                        }
                    }
                }
            }

            // Invoke success callback since resync was successful
            if (!string.IsNullOrEmpty(message.Id))
            {
                context.ServerController.InvokeResponseSuccess(message.Id, payload);
            }
        }

    }
}
