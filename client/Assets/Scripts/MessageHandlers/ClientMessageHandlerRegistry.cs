using System;
using System.Collections.Generic;
using NullZustand;
using UnityEngine;

namespace ClientMessageHandlers
{
    public class ClientMessageHandlerRegistry
    {
        private readonly Dictionary<string, IClientMessageHandler> _handlersByRequest = new Dictionary<string, IClientMessageHandler>();
        private readonly Dictionary<string, IClientMessageHandler> _handlersByResponse = new Dictionary<string, IClientMessageHandler>();

        public void RegisterHandler(IClientMessageHandler handler)
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            if (handler.RequestMessageType != null)
                _handlersByRequest[handler.RequestMessageType] = handler;

            if (handler.ResponseMessageType != null)
                _handlersByResponse[handler.ResponseMessageType] = handler;

            if (handler.BroadcastMessageType != null)
                _handlersByResponse[handler.BroadcastMessageType] = handler;
        }

        public bool ProcessMessage(Message message, ServerController serverController)
        {
            if (_handlersByResponse.TryGetValue(message.Type, out IClientMessageHandler handler))
            {
                try
                {
                    handler.HandleResponse(message, serverController);
                    return true;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[CLIENT_HANDLER] Failed to handle {message.Type}: {ex.Message}");
                    return false;
                }
            }

            return false;
        }

        public T GetHandler<T>(string requestMessageType) where T : class, IClientMessageHandler
        {
            if (_handlersByRequest.TryGetValue(requestMessageType, out IClientMessageHandler handler))
            {
                return handler as T;
            }
            return null;
        }
    }
}

