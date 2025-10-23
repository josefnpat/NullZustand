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
                // Update lastLocationUpdateId
                if (payload["lastLocationUpdateId"] != null)
                {
                    long updateId = payload["lastLocationUpdateId"].Value<long>();
                    serverController.SetLastLocationUpdateId(updateId);
                }

                // Load all player locations
                if (payload["allPlayers"] != null)
                {
                    var allPlayers = payload["allPlayers"] as JArray;
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
                        playerObj.CurrentState = new PlayerState(position, rotation, velocity, timestampMs);
                        serverController.TriggerPlayerUpdate(playerObj);
                    }
                }

                serverController.InvokePlayerAuthenticate();

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

