using System;
using System.Threading.Tasks;
using NullZustand;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace ClientMessageHandlers.Handlers
{
    public class LoginHandlerMessageHandler : ClientMessageHandler, IClientMessageHandler<string, string>
    {
        public override string RequestMessageType => MessageTypes.LOGIN_REQUEST;
        public override string ResponseMessageType => MessageTypes.LOGIN_RESPONSE;

        public async Task<string> SendRequestAsync(ServerController serverController, string username, string password, Action<object> onSuccess = null, Action<string> onFailure = null)
        {
            string messageId = GenerateMessageId();
            serverController.RegisterResponseCallbacks(messageId, onSuccess, onFailure);

            await serverController.SendMessageAsync(new Message
            {
                Id = messageId,
                Type = MessageTypes.LOGIN_REQUEST,
                Payload = new { username = username, password = password }
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
                // Update lastLocationUpdateId
                if (payload["lastLocationUpdateId"] != null)
                {
                    long updateId = payload["lastLocationUpdateId"].Value<long>();
                    context.ServerController.SetLastLocationUpdateId(updateId);
                }

                // Load all player locations
                if (payload["allPlayers"] != null)
                {
                    var allPlayers = payload["allPlayers"] as JArray;
                    Player currentPlayer = context.ServerController.GetCurrentPlayer();

                    foreach (var player in allPlayers)
                    {
                        string username = player["username"].Value<string>();

                        float x = player["x"]?.Value<float>() ?? 0f;
                        float y = player["y"]?.Value<float>() ?? 0f;
                        float z = player["z"]?.Value<float>() ?? 0f;
                        float rotX = player["rotX"]?.Value<float>() ?? 0f;
                        float rotY = player["rotY"]?.Value<float>() ?? 0f;
                        float rotZ = player["rotZ"]?.Value<float>() ?? 0f;
                        float rotW = player["rotW"]?.Value<float>() ?? 1f;
                        float velocity = player["velocity"]?.Value<float>() ?? 0f;
                        long timestampMs = player["timestampMs"]?.Value<long>() ?? 0L;

                        var position = new Vector3(x, y, z);
                        var rotation = new Quaternion(rotX, rotY, rotZ, rotW);
                        var playerObj = new Player(username);

                        long entityId = player["entityId"]?.Value<long>() ?? EntityManager.INVALID_ENTITY_ID;
                        if (entityId != EntityManager.INVALID_ENTITY_ID)
                        {
                            playerObj.EntityId = entityId;
                            context.EntityManager.CreateOrUpdateEntity(entityId, EntityType.Player, position, rotation, velocity, timestampMs);
                            if (currentPlayer != null && currentPlayer.Username == username)
                            {
                                currentPlayer.EntityId = entityId;
                            }
                        }

                        context.ServerController.TriggerPlayerUpdate(playerObj);
                    }
                }

                context.ServerController.InvokePlayerAuthenticate();

                context.ServerController.InvokeResponseSuccess(message.Id, payload);
            }
            else
            {
                string error = payload["error"]?.Value<string>() ?? "Unknown error";
                context.ServerController.InvokeResponseFailure(message.Id, error);
            }
        }
    }
}

