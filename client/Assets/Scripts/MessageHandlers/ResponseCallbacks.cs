using System;
using System.Collections.Generic;

namespace ClientMessageHandlers
{
    public class ResponseCallbacks
    {
        private readonly Dictionary<string, Action<object>> _successCallbacks = new Dictionary<string, Action<object>>();
        private readonly Dictionary<string, Action<string>> _failureCallbacks = new Dictionary<string, Action<string>>();

        public void RegisterCallbacks(string messageId, Action<object> onSuccess, Action<string> onFailure)
        {
            if (onSuccess != null)
                _successCallbacks[messageId] = onSuccess;

            if (onFailure != null)
                _failureCallbacks[messageId] = onFailure;
        }

        public void InvokeSuccess(string messageId, object payload)
        {
            if (_successCallbacks.TryGetValue(messageId, out Action<object> callback))
            {
                callback?.Invoke(payload);
                ClearCallbacks(messageId);
            }
        }

        public void InvokeFailure(string messageId, string error)
        {
            if (_failureCallbacks.TryGetValue(messageId, out Action<string> callback))
            {
                callback?.Invoke(error);
                ClearCallbacks(messageId);
            }
        }

        private void ClearCallbacks(string messageId)
        {
            _successCallbacks.Remove(messageId);
            _failureCallbacks.Remove(messageId);
        }
    }
}

