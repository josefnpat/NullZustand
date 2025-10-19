using System.Net.Sockets;
using System.Threading.Tasks;

namespace NullZustand.MessageHandlers
{
    public interface IMessageHandler
    {
        string MessageType { get; }
        Task HandleAsync(Message message, NetworkStream stream);
    }
}
