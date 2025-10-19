using System;
using NullZustand;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace ClientMessageHandlers
{
    public abstract class ClientHandler : IClientHandler
    {
        public abstract string RequestMessageType { get; }
        public abstract string ResponseMessageType { get; }

        public abstract void HandleResponse(Message message, ServerController serverController);

        protected string GenerateMessageId()
        {
            return Guid.NewGuid().ToString("N").Substring(0, 16);
        }

        protected JObject GetPayloadAsJObject(Message message)
        {
            if (message.Payload == null)
                return null;

            try
            {
                return JObject.FromObject(message.Payload);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to parse payload for {ResponseMessageType}: {ex.Message}");
                return null;
            }
        }
    }
}

