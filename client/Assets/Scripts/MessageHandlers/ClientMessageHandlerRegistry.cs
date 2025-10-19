using System;
using System.Collections.Generic;
using NullZustand;
using UnityEngine;

namespace ClientMessageHandlers
{
    public class ClientMessageHandlerRegistry
    {
        private readonly Dictionary<string, IClientHandler> _handlersByRequest = new Dictionary<string, IClientHandler>();
        private readonly Dictionary<string, IClientHandler> _handlersByResponse = new Dictionary<string, IClientHandler>();

        public void RegisterHandler(IClientHandler handler)
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            _handlersByRequest[handler.RequestMessageType] = handler;
            _handlersByResponse[handler.ResponseMessageType] = handler;
        }

        public bool ProcessMessage(Message message, ServerController serverController)
        {
            if (_handlersByResponse.TryGetValue(message.Type, out IClientHandler handler))
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

        public T GetHandler<T>(string requestMessageType) where T : class, IClientHandler
        {
            if (_handlersByRequest.TryGetValue(requestMessageType, out IClientHandler handler))
            {
                return handler as T;
            }
            return null;
        }
    }
}

