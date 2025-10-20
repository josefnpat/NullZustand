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
                // Check if user is already logged in from another session
                ClientSession existingSession = _sessionManager.GetExistingSessionForUsername(payload.username);
                if (existingSession != null && existingSession.SessionId != session.SessionId)
                {
                    Console.WriteLine($"[LOGIN] User '{payload.username}' already logged in from session {existingSession.SessionId}. Kicking old session.");

                    // Notify the old session that it's being disconnected
                    try
                    {
                        await SendAsync(existingSession, new Message
                        {
                            Type = MessageTypes.ERROR,
                            Payload = new
                            {
                                code = "LOGGED_IN_ELSEWHERE",
                                message = "You have been logged in from another location."
                            }
                        });
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[LOGIN] Failed to notify old session: {ex.Message}");
                    }

                    // The old session will be cleaned up when the client disconnects
                    // or we can force close the stream here
                    try
                    {
                        existingSession.Stream?.Close();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[LOGIN] Failed to close old session stream: {ex.Message}");
                    }
                }

                // Authenticate the new session
                _sessionManager.AuthenticateSession(session.SessionId, payload.username);
                Console.WriteLine($"[LOGIN] User '{payload.username}' logged in successfully");

                // Get all player locations and the current update ID
                var allPlayers = _playerManager.GetAllPlayerLocations();
                long currentUpdateId = _playerManager.GetCurrentUpdateId();

                // Client can find their own position in allPlayers array
                await SendResponseAsync(session, message, MessageTypes.LOGIN_RESPONSE, new
                {
                    success = true,
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
