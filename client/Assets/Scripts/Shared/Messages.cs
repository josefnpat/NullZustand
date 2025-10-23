using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace NullZustand
{
    [Serializable]
    public class Message
    {
        public string Id { get; set; }
        public string Type { get; set; }
        public object Payload { get; set; }
    }

    public static class MessageTypes
    {
        public const string CHAT_MESSAGE_REQUEST = "ChatMessageRequest";
        public const string CHAT_MESSAGE_RESPONSE = "ChatMessageResponse";
        public const string ERROR = "Error";
        public const string LOCATION_UPDATES_BROADCAST = "LocationUpdatesBroadcast";
        public const string LOCATION_UPDATES_REQUEST = "LocationUpdatesRequest";
        public const string LOCATION_UPDATES_RESPONSE = "LocationUpdatesResponse";
        public const string LOGIN_REQUEST = "LoginRequest";
        public const string LOGIN_RESPONSE = "LoginResponse";
        public const string PING = "Ping";
        public const string PONG = "Pong";
        public const string REGISTER_REQUEST = "RegisterRequest";
        public const string REGISTER_RESPONSE = "RegisterResponse";
        public const string TIME_SYNC_REQUEST = "TimeSyncRequest";
        public const string TIME_SYNC_RESPONSE = "TimeSyncResponse";
        public const string UPDATE_POSITION_REQUEST = "UpdatePositionRequest";
        public const string UPDATE_POSITION_RESPONSE = "UpdatePositionResponse";
    }

    public static class ServerConstants
    {
        public const int DEFAULT_PORT = 8140;
        public const int BUFFER_SIZE = 4096;

        public const int SOCKET_READ_TIMEOUT_MS = 60000; // 60 seconds
        public const int SOCKET_WRITE_TIMEOUT_MS = 30000; // 30 seconds
        public const int IDLE_SESSION_TIMEOUT_SECONDS = 300; // 5 minutes
        public const int IDLE_CLEANUP_INTERVAL_SECONDS = 60; // 1 minute
    }

    public static class ValidationConstants
    {
        // Username validation
        public const int MIN_USERNAME_LENGTH = 3;
        public const int MAX_USERNAME_LENGTH = 50;

        // Password validation
        public const int MIN_PASSWORD_LENGTH = 4;
        public const int MAX_PASSWORD_LENGTH = 100;

        // Coordinate validation
        public const float MIN_COORDINATE = -100000f;
        public const float MAX_COORDINATE = 100000f;
    }

    public static class TimeUtils
    {
        private static readonly DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public static long GetUnixTimestampMs()
        {
            return (long)(DateTime.UtcNow - UnixEpoch).TotalMilliseconds;
        }

        public static DateTime FromUnixTimestampMs(long timestampMs)
        {
            return UnixEpoch.AddMilliseconds(timestampMs);
        }
    }

    public static class MessageFraming
    {
        private static async Task<bool> ReadExactAsync(Stream stream, byte[] buffer, int offset, int count)
        {
            int totalRead = 0;
            while (totalRead < count)
            {
                int bytesRead = await stream.ReadAsync(buffer, offset + totalRead, count - totalRead);
                if (bytesRead == 0)
                {
                    return false;
                }
                totalRead += bytesRead;
            }
            return true;
        }

        public static async Task<string> ReadMessageAsync(Stream stream)
        {
            try
            {
                byte[] lengthBuffer = new byte[4];
                bool success = await ReadExactAsync(stream, lengthBuffer, 0, 4);
                if (!success)
                {
                    return null;
                }

                if (BitConverter.IsLittleEndian)
                {
                    Array.Reverse(lengthBuffer);
                }
                int messageLength = BitConverter.ToInt32(lengthBuffer, 0);

                if (messageLength <= 0 || messageLength > ServerConstants.BUFFER_SIZE)
                {
                    throw new InvalidOperationException($"Invalid message length: {messageLength}");
                }

                byte[] messageBuffer = new byte[messageLength];
                success = await ReadExactAsync(stream, messageBuffer, 0, messageLength);
                if (!success)
                {
                    return null;
                }

                string message = Encoding.UTF8.GetString(messageBuffer);
                return message;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static async Task WriteMessageAsync(Stream stream, string message)
        {
            byte[] messageBytes = Encoding.UTF8.GetBytes(message);
            int messageLength = messageBytes.Length;

            byte[] lengthPrefix = BitConverter.GetBytes(messageLength);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(lengthPrefix);
            }

            await stream.WriteAsync(lengthPrefix, 0, 4);
            await stream.WriteAsync(messageBytes, 0, messageBytes.Length);
        }
    }
}
