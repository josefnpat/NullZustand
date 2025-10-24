using System;
using System.Threading.Tasks;
using NullZustand;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace ClientMessageHandlers.Handlers
{
    public class TimeSyncMessageHandler : ClientMessageHandler, IClientMessageHandlerNoParam
    {
        public override string RequestMessageType => MessageTypes.TIME_SYNC_REQUEST;
        public override string ResponseMessageType => MessageTypes.TIME_SYNC_RESPONSE;

        public async Task<string> SendRequestAsync(ServerController serverController, Action<object> onSuccess = null, Action<string> onFailure = null)
        {
            string messageId = GenerateMessageId();
            serverController.RegisterResponseCallbacks(messageId, onSuccess, onFailure);

            long clientSendTime = TimeUtils.GetUnixTimestampMs();

            await serverController.SendMessageAsync(new Message
            {
                Id = messageId,
                Type = MessageTypes.TIME_SYNC_REQUEST,
                Payload = new { clientSendTime = clientSendTime }
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

            long clientReceiveTime = TimeUtils.GetUnixTimestampMs();
            long clientSendTime = payload["clientSendTime"]?.Value<long>() ?? 0;
            long serverReceiveTime = payload["serverReceiveTime"]?.Value<long>() ?? 0;
            long serverSendTime = payload["serverSendTime"]?.Value<long>() ?? 0;

            // Calculate round-trip time and clock offset
            long roundTripTime = clientReceiveTime - clientSendTime;
            long oneWayLatency = roundTripTime / 2;
            long serverClockOffset = serverReceiveTime - clientSendTime - oneWayLatency;

            Debug.Log($"[TIME_SYNC] RTT: {roundTripTime}ms, Latency: {oneWayLatency}ms, Offset: {serverClockOffset}ms");

            // Update the server controller's clock offset
            context.ServerController.SetServerClockOffset(serverClockOffset);

            // Invoke success callback
            context.ServerController.InvokeResponseSuccess(message.Id, new
            {
                roundTripTime = roundTripTime,
                oneWayLatency = oneWayLatency,
                serverClockOffset = serverClockOffset
            });
        }
    }
}

