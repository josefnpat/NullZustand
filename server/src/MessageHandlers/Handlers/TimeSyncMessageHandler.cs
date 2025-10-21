using System;
using System.Threading.Tasks;

namespace NullZustand.MessageHandlers.Handlers
{
    public class TimeSyncRequestPayload
    {
        public long clientSendTime { get; set; }
    }

    public class TimeSyncMessageHandler : MessageHandler
    {
        public override string MessageType => MessageTypes.TIME_SYNC_REQUEST;

        // Does not require authentication - clients can sync time before login
        public override bool RequiresAuthentication => false;

        public override async Task HandleAsync(Message message, ClientSession session)
        {
            var payload = GetPayload<TimeSyncRequestPayload>(message);

            if (payload == null)
            {
                Console.WriteLine($"[WARNING] TimeSyncRequest received with null payload from {session.SessionId}");
                return;
            }

            // Get current server time in milliseconds (Unix timestamp)
            long serverReceiveTime = TimeUtils.GetUnixTimestampMs();
            long serverSendTime = TimeUtils.GetUnixTimestampMs();

            Console.WriteLine($"[TIME_SYNC] Request from {session.SessionId} - Client: {payload.clientSendTime}, Server: {serverReceiveTime}");

            // Send response with timing data
            await SendResponseAsync(session, message, MessageTypes.TIME_SYNC_RESPONSE, new
            {
                clientSendTime = payload.clientSendTime,
                serverReceiveTime = serverReceiveTime,
                serverSendTime = serverSendTime
            });
        }
    }
}

