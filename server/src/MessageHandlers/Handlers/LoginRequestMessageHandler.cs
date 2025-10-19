using System;
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
        private readonly SessionManager _sessionManager;
        private readonly UserAccountManager _accountManager;
        private readonly PlayerManager _playerManager;

        public LoginRequestMessageHandler(SessionManager sessionManager, UserAccountManager accountManager, PlayerManager playerManager)
        {
            _sessionManager = sessionManager ?? throw new ArgumentNullException(nameof(sessionManager));
            _accountManager = accountManager ?? throw new ArgumentNullException(nameof(accountManager));
            _playerManager = playerManager ?? throw new ArgumentNullException(nameof(playerManager));
        }

        public override string MessageType => MessageTypes.LOGIN_REQUEST;

        public override async Task HandleAsync(Message message, ClientSession session)
        {
            // Extract and validate the payload
            var payload = GetPayload<LoginRequestPayload>(message);

            if (payload == null)
            {
                Console.WriteLine("[WARNING] LoginRequest received with null payload");
                await SendResponseAsync(session, message, MessageTypes.LOGIN_RESPONSE,
                    new { success = false, error = "Invalid payload" });
                return;
            }

            // Validate input lengths to prevent abuse
            if (payload.username != null && payload.username.Length > ValidationConstants.MAX_USERNAME_LENGTH)
            {
                Console.WriteLine($"[LOGIN] Rejected overly long username from {session.SessionId}");
                await SendResponseAsync(session, message, MessageTypes.LOGIN_RESPONSE,
                    new { success = false, error = "Invalid username or password" });
                return;
            }

            if (payload.password != null && payload.password.Length > ValidationConstants.MAX_PASSWORD_LENGTH)
            {
                Console.WriteLine($"[LOGIN] Rejected overly long password from {session.SessionId}");
                await SendResponseAsync(session, message, MessageTypes.LOGIN_RESPONSE,
                    new { success = false, error = "Invalid username or password" });
                return;
            }

            Console.WriteLine($"[LOGIN] Session {session.SessionId} - User '{payload.username}' attempting to login");

            bool isValid = _accountManager.ValidateCredentials(payload.username, payload.password);

            if (isValid)
            {
                _sessionManager.AuthenticateSession(session.SessionId, payload.username);

                string sessionToken = Guid.NewGuid().ToString("N").Substring(0, 16);
                Console.WriteLine($"[LOGIN] User '{payload.username}' logged in successfully with token: {sessionToken}");

                // Get all player locations and the current update ID
                var allPlayers = _playerManager.GetAllPlayerLocations();
                long currentUpdateId = _playerManager.GetCurrentUpdateId();

                // Client can find their own position in allPlayers array
                await SendResponseAsync(session, message, MessageTypes.LOGIN_RESPONSE, new
                {
                    success = true,
                    sessionToken = sessionToken,
                    allPlayers = allPlayers,
                    lastLocationUpdateId = currentUpdateId
                });
            }
            else
            {
                Console.WriteLine($"[LOGIN] User '{payload.username}' login failed - invalid credentials");

                await SendResponseAsync(session, message, MessageTypes.LOGIN_RESPONSE,
                    new { success = false, error = "Invalid username or password" });
            }
        }
    }
}
