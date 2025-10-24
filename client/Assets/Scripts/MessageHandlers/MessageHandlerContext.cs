namespace ClientMessageHandlers
{
    public class MessageHandlerContext
    {
        public ServerController ServerController { get; }
        public EntityManager EntityManager { get; }
        public PlayerManager PlayerManager { get; }

        public MessageHandlerContext(ServerController serverController, EntityManager entityManager, PlayerManager playerManager)
        {
            ServerController = serverController ?? throw new System.ArgumentNullException(nameof(serverController));
            EntityManager = entityManager ?? throw new System.ArgumentNullException(nameof(entityManager));
            PlayerManager = playerManager ?? throw new System.ArgumentNullException(nameof(playerManager));
        }
    }
}
