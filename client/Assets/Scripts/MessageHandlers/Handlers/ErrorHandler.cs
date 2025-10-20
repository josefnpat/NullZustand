using System;
using NullZustand;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace ClientMessageHandlers.Handlers
{
    public class ErrorHandler : ClientHandler, IClientHandlerNoParam
    {
        public override string RequestMessageType => MessageTypes.ERROR; // Not used for error
        public override string ResponseMessageType => MessageTypes.ERROR;

        public System.Threading.Tasks.Task<string> SendRequestAsync(ServerController serverController, Action<object> onSuccess = null, Action<string> onFailure = null)
        {
            // Errors are only received, never sent by client
            return System.Threading.Tasks.Task.FromResult<string>(null);
        }

        public override void HandleResponse(Message message, ServerController serverController)
        {
            JObject payload = GetPayloadAsJObject(message);
            if (payload == null)
            {
                Debug.LogWarning($"[{ResponseMessageType}] Received null or invalid payload");
                return;
            }

            string code = payload["code"]?.Value<string>() ?? "UNKNOWN_ERROR";
            string errorMessage = payload["message"]?.Value<string>() ?? "An error occurred";

            // Special handling for RESYNC_REQUIRED error
            if (code == "RESYNC_REQUIRED")
            {
                Debug.LogWarning($"[ErrorHandler] Resync required - applying full location data");

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
                        float x = player["x"].Value<float>();
                        float y = player["y"].Value<float>();
                        float z = player["z"].Value<float>();

                        var position = new Vector3(x, y, z);
                        serverController.UpdatePlayerLocation(username, position);
                    }
                }

                // Invoke success callback since resync was successful
                if (!string.IsNullOrEmpty(message.Id))
                {
                    serverController.InvokeResponseSuccess(message.Id, payload);
                }

                return;
            }

            // Special handling for LOGGED_IN_ELSEWHERE error
            if (code == "LOGGED_IN_ELSEWHERE")
            {
                Debug.LogWarning($"[ErrorHandler] Logged in from another location - disconnecting");
                // The connection will be closed by the server
                // Just notify the user through the error event
                serverController.InvokeError(code, errorMessage);
                return;
            }

            // Standard error handling for other error codes
            // Invoke the OnError event for global error handling
            serverController.InvokeError(code, errorMessage);

            // If this error is in response to a specific message, invoke the failure callback
            if (!string.IsNullOrEmpty(message.Id))
            {
                serverController.InvokeResponseFailure(message.Id, errorMessage);
            }
        }
    }
}

