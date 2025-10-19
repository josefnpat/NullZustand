using System;

namespace NullZustand
{
    /// <summary>
    /// Shared message types and constants used by both Unity client and server.
    /// This file is referenced by the server build process via the Makefile.
    /// Any changes to message types or constants will be automatically reflected in both projects.
    /// </summary>
    
    [Serializable]
    public class Message
    {
        public string Type { get; set; }
        public object Payload { get; set; }
    }

    public static class MessageTypes
    {
        public const string PING = "Ping";
        public const string PONG = "Pong";
        public const string LOGIN_REQUEST = "LoginRequest";
        public const string LOGIN_RESPONSE = "LoginResponse";
    }

    public static class ServerConstants
    {
        public const int DEFAULT_PORT = 8140;
        public const int BUFFER_SIZE = 4096;
    }
}
