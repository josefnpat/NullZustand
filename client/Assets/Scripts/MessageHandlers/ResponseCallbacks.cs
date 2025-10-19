using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ClientMessageHandlers
{
    public class ResponseCallbacks
    {
        private readonly Dictionary<string, Action<object>> _successCallbacks = new Dictionary<string, Action<object>>();
        private readonly Dictionary<string, Action<string>> _failureCallbacks = new Dictionary<string, Action<string>>();
        private readonly Dictionary<string, float> _callbackTimestamps = new Dictionary<string, float>();

        // Default timeout: 30 seconds
        private const float DEFAULT_TIMEOUT_SECONDS = 30f;

        public void RegisterCallbacks(string messageId, Action<object> onSuccess, Action<string> onFailure)
        {
            _callbackTimestamps[messageId] = Time.unscaledTime;

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

        public int CleanupExpiredCallbacks(float timeoutSeconds = DEFAULT_TIMEOUT_SECONDS)
        {
            float currentTime = Time.unscaledTime;

            // Find all expired callback IDs
            var expiredIds = _callbackTimestamps
                .Where(kvp => currentTime - kvp.Value > timeoutSeconds)
                .Select(kvp => kvp.Key)
                .ToList();

            // Invoke failure callbacks for expired requests
            foreach (var messageId in expiredIds)
            {
                if (_failureCallbacks.TryGetValue(messageId, out Action<string> failureCallback))
                {
                    failureCallback?.Invoke("Request timed out");
                }

                ClearCallbacks(messageId);

                Debug.LogWarning($"[ResponseCallbacks] Cleaned up expired callback for message ID: {messageId}");
            }

            return expiredIds.Count;
        }

        private void ClearCallbacks(string messageId)
        {
            _successCallbacks.Remove(messageId);
            _failureCallbacks.Remove(messageId);
            _callbackTimestamps.Remove(messageId);
        }
    }
}

