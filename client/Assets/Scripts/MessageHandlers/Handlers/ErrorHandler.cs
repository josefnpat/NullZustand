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

