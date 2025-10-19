using System;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace NullZustand.MessageHandlers.Handlers
{
    public class LoginRequestPayload
    {
        public string username { get; set; }
        public string password { get; set; }
    }

    public class LoginRequestMessageHandler : MessageHandler
    {
        public override string MessageType => MessageTypes.LOGIN_REQUEST;

        public override async Task HandleAsync(Message message, NetworkStream stream)
        {
            // Extract and validate the payload
            var payload = GetPayload<LoginRequestPayload>(message);

            if (payload == null)
            {
                Console.WriteLine("[WARNING] LoginRequest received with null payload");
                await SendAsync(stream, new Message
                {
                    Type = MessageTypes.LOGIN_RESPONSE,
                    Payload = new { success = false, error = "Invalid payload" }
                });
                return;
            }

            Console.WriteLine($"[LOGIN] User '{payload.username}' attempting to login");

            // TODO: Implement actual authentication logic here
            // For now, accept any non-empty username
            bool isValid = !string.IsNullOrEmpty(payload.username) && !string.IsNullOrEmpty(payload.password);

            if (isValid)
            {
                string sessionToken = Guid.NewGuid().ToString("N").Substring(0, 16);
                Console.WriteLine($"[LOGIN] User '{payload.username}' logged in successfully with token: {sessionToken}");

                await SendAsync(stream, new Message
                {
                    Type = MessageTypes.LOGIN_RESPONSE,
                    Payload = new { success = true, sessionToken = sessionToken, username = payload.username }
                });
            }
            else
            {
                Console.WriteLine($"[LOGIN] User '{payload.username}' login failed - invalid credentials");

                await SendAsync(stream, new Message
                {
                    Type = MessageTypes.LOGIN_RESPONSE,
                    Payload = new { success = false, error = "Invalid username or password" }
                });
            }
        }
    }
}
