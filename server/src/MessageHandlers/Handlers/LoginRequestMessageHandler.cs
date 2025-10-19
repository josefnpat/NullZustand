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
                await SendAsync(session, new Message
                {
                    Type = MessageTypes.LOGIN_RESPONSE,
                    Payload = new { success = false, error = "Invalid payload" }
                });
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
                await SendAsync(session, new Message
                {
                    Type = MessageTypes.LOGIN_RESPONSE,
                    Payload = new
                    {
                        success = true,
                        sessionToken = sessionToken,
                        allPlayers = allPlayers,
                        lastLocationUpdateId = currentUpdateId
                    }
                });
            }
            else
            {
                Console.WriteLine($"[LOGIN] User '{payload.username}' login failed - invalid credentials");

                await SendAsync(session, new Message
                {
                    Type = MessageTypes.LOGIN_RESPONSE,
                    Payload = new { success = false, error = "Invalid username or password" }
                });
            }
        }
    }
}
