using System;
using NullZustand;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace ClientMessageHandlers.Handlers
{
    public class ErrorMessageHandler : ClientMessageHandler, IClientMessageHandlerNoParam
    {
        public override string RequestMessageType => null; // Not used for error
        public override string ResponseMessageType => MessageTypes.ERROR;

        public System.Threading.Tasks.Task<string> SendRequestAsync(ServerController serverController, Action<object> onSuccess = null, Action<string> onFailure = null)
        {
            // Errors are only received, never sent by client
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

            string code = payload["code"]?.Value<string>() ?? "UNKNOWN_ERROR";
            string errorMessage = payload["message"]?.Value<string>() ?? "An error occurred";

            // Special handling for RESYNC_REQUIRED error
            if (code == "RESYNC_REQUIRED")
            {
                // Delegate to ResyncRequiredMessageHandler
                var resyncHandler = new ResyncRequiredMessageHandler();
                resyncHandler.HandleResponse(message, context);
                return;
            }

            // Special handling for LOGGED_IN_ELSEWHERE error
            if (code == "LOGGED_IN_ELSEWHERE")
            {
                Debug.LogWarning($"[ErrorHandler] Logged in from another location - disconnecting");
                // The connection will be closed by the server
                // Just notify the user through the error event
                context.ServerController.InvokeError(code, errorMessage);
                return;
            }

            // Standard error handling for other error codes
            // Invoke the OnError event for global error handling
            context.ServerController.InvokeError(code, errorMessage);

            // If this error is in response to a specific message, invoke the failure callback
            if (!string.IsNullOrEmpty(message.Id))
            {
                context.ServerController.InvokeResponseFailure(message.Id, errorMessage);
            }
        }
    }
}

