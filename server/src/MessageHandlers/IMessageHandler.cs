using System.Threading.Tasks;

namespace NullZustand.MessageHandlers
{
    public interface IMessageHandler
    {
        string MessageType { get; }
        bool RequiresAuthentication { get; }
        Task HandleAsync(Message message, ClientSession session);
    }
}
