using System;
using System.Threading.Tasks;

namespace NullZustand.MessageHandlers.Handlers
{
    public class RegisterRequestPayload
    {
        public string username { get; set; }
        public string password { get; set; }
    }

    public class RegisterRequestMessageHandler : MessageHandler
    {
        private readonly UserAccountManager _accountManager;

        public RegisterRequestMessageHandler(UserAccountManager accountManager)
        {
            _accountManager = accountManager ?? throw new ArgumentNullException(nameof(accountManager));
        }

        public override string MessageType => MessageTypes.REGISTER_REQUEST;

        public override async Task HandleAsync(Message message, ClientSession session)
        {
            // Extract and validate the payload
            var payload = GetPayload<RegisterRequestPayload>(message);

            if (payload == null)
            {
                Console.WriteLine("[WARNING] RegisterRequest received with null payload");
                await SendAsync(session, new Message
                {
                    Type = MessageTypes.REGISTER_RESPONSE,
                    Payload = new { success = false, error = "Invalid payload" }
                });
                return;
            }

            Console.WriteLine($"[REGISTER] Session {session.SessionId} - Attempting to register user '{payload.username}'");

            // Attempt to register the user
            bool success = _accountManager.RegisterUser(payload.username, payload.password, out string error);

            if (success)
            {
                Console.WriteLine($"[REGISTER] User '{payload.username}' registered successfully");

                await SendAsync(session, new Message
                {
                    Type = MessageTypes.REGISTER_RESPONSE,
                    Payload = new { success = true, username = payload.username }
                });
            }
            else
            {
                Console.WriteLine($"[REGISTER] Registration failed for '{payload.username}': {error}");

                await SendAsync(session, new Message
                {
                    Type = MessageTypes.REGISTER_RESPONSE,
                    Payload = new { success = false, error = error }
                });
            }
        }
    }
}

