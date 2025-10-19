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

        private bool ContainsInvalidCharacters(string input)
        {
            if (string.IsNullOrEmpty(input))
                return false;

            // Check for control characters that could cause issues
            foreach (char c in input)
            {
                if (char.IsControl(c))
                    return true;
            }
            return false;
        }

        public override async Task HandleAsync(Message message, ClientSession session)
        {
            // Extract and validate the payload
            var payload = GetPayload<RegisterRequestPayload>(message);

            if (payload == null)
            {
                Console.WriteLine("[WARNING] RegisterRequest received with null payload");
                await SendResponseAsync(session, message, MessageTypes.REGISTER_RESPONSE,
                    new { success = false, error = "Invalid payload" });
                return;
            }

            // Validate input lengths to prevent abuse
            if (payload.username != null && payload.username.Length > ValidationConstants.MAX_USERNAME_LENGTH)
            {
                Console.WriteLine($"[REGISTER] Rejected overly long username from {session.SessionId}");
                await SendResponseAsync(session, message, MessageTypes.REGISTER_RESPONSE,
                    new { success = false, error = $"Username must be at most {ValidationConstants.MAX_USERNAME_LENGTH} characters" });
                return;
            }

            if (payload.password != null && payload.password.Length > ValidationConstants.MAX_PASSWORD_LENGTH)
            {
                Console.WriteLine($"[REGISTER] Rejected overly long password from {session.SessionId}");
                await SendResponseAsync(session, message, MessageTypes.REGISTER_RESPONSE,
                    new { success = false, error = $"Password must be at most {ValidationConstants.MAX_PASSWORD_LENGTH} characters" });
                return;
            }

            // Check for invalid characters
            if (ContainsInvalidCharacters(payload.username))
            {
                Console.WriteLine($"[REGISTER] Rejected username with control characters from {session.SessionId}");
                await SendResponseAsync(session, message, MessageTypes.REGISTER_RESPONSE,
                    new { success = false, error = "Username contains invalid characters" });
                return;
            }

            Console.WriteLine($"[REGISTER] Session {session.SessionId} - Attempting to register user '{payload.username}'");

            // Attempt to register the user
            bool success = _accountManager.RegisterUser(payload.username, payload.password, out string error);

            if (success)
            {
                Console.WriteLine($"[REGISTER] User '{payload.username}' registered successfully");

                await SendResponseAsync(session, message, MessageTypes.REGISTER_RESPONSE,
                    new { success = true, username = payload.username });
            }
            else
            {
                Console.WriteLine($"[REGISTER] Registration failed for '{payload.username}': {error}");

                await SendResponseAsync(session, message, MessageTypes.REGISTER_RESPONSE,
                    new { success = false, error = error });
            }
        }
    }
}

