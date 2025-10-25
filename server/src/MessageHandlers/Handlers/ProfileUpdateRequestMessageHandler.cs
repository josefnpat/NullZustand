using System;
using System.Threading.Tasks;

namespace NullZustand.MessageHandlers.Handlers
{
    public class ProfileUpdateRequestPayload
    {
        public string displayName { get; set; }
        public int profileImage { get; set; }
    }

    public class ProfileUpdateRequestMessageHandler : MessageHandler
    {
        private readonly PlayerManager _playerManager;
        private readonly SessionManager _sessionManager;

        public ProfileUpdateRequestMessageHandler(PlayerManager playerManager, SessionManager sessionManager)
        {
            _playerManager = playerManager ?? throw new ArgumentNullException(nameof(playerManager));
            _sessionManager = sessionManager ?? throw new ArgumentNullException(nameof(sessionManager));
        }

        public override string MessageType => MessageTypes.PROFILE_UPDATE_REQUEST;
        public override bool RequiresAuthentication => true;

        public override async Task HandleAsync(Message message, ClientSession session)
        {
            var payload = GetPayload<ProfileUpdateRequestPayload>(message);

            if (payload == null)
            {
                Console.WriteLine("[WARNING] ProfileUpdateRequest received with null payload");
                await SendResponseAsync(session, message, MessageTypes.PROFILE_UPDATE_RESPONSE,
                    new { success = false, error = "Invalid payload" });
                return;
            }

            if (string.IsNullOrWhiteSpace(payload.displayName))
            {
                await SendResponseAsync(session, message, MessageTypes.PROFILE_UPDATE_RESPONSE,
                    new { success = false, error = "Display name cannot be empty" });
                return;
            }

            if (payload.displayName.Length > ValidationConstants.MAX_USERNAME_LENGTH)
            {
                await SendResponseAsync(session, message, MessageTypes.PROFILE_UPDATE_RESPONSE,
                    new { success = false, error = $"Display name must be at most {ValidationConstants.MAX_USERNAME_LENGTH} characters" });
                return;
            }

            if (payload.profileImage < -1)
            {
                await SendResponseAsync(session, message, MessageTypes.PROFILE_UPDATE_RESPONSE,
                    new { success = false, error = "Profile image must be -1 or greater" });
                return;
            }

            try
            {
                var player = _playerManager.GetOrCreatePlayer(session.Username);
                player.Profile.DisplayName = payload.displayName;
                player.Profile.ProfileImage = payload.profileImage;

                Console.WriteLine($"[PROFILE] Updated profile for {session.Username}: displayName={payload.displayName}, profileImage={payload.profileImage}");

                await SendResponseAsync(session, message, MessageTypes.PROFILE_UPDATE_RESPONSE,
                    new { success = true });

                await BroadcastProfileUpdateAsync(session.Username, payload.displayName, payload.profileImage);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to update profile for {session.Username}: {ex.Message}");
                await SendResponseAsync(session, message, MessageTypes.PROFILE_UPDATE_RESPONSE,
                    new { success = false, error = "Failed to update profile" });
            }
        }

        private async Task BroadcastProfileUpdateAsync(string username, string displayName, int profileImage)
        {
            var broadcastMessage = new Message
            {
                Id = Guid.NewGuid().ToString("N"),
                Type = MessageTypes.PROFILE_UPDATE_BROADCAST,
                Payload = new
                {
                    username = username,
                    displayName = displayName,
                    profileImage = profileImage
                }
            };

            await _sessionManager.BroadcastToAllSessionsAsync(broadcastMessage);
        }
    }
}
