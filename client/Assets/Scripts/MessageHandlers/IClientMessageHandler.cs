using System;
using System.Threading.Tasks;
using NullZustand;

namespace ClientMessageHandlers
{
    public interface IClientMessageHandler
    {
        string RequestMessageType { get; }
        string ResponseMessageType { get; }
        string BroadcastMessageType { get; }
        void HandleResponse(Message message, MessageHandlerContext context);
    }

    public interface IClientMessageHandlerNoParam : IClientMessageHandler
    {
        Task<string> SendRequestAsync(ServerController serverController, Action<object> onSuccess = null, Action<string> onFailure = null);
    }

    public interface IClientMessageHandler<in T> : IClientMessageHandler
    {
        Task<string> SendRequestAsync(ServerController serverController, T data, Action<object> onSuccess = null, Action<string> onFailure = null);
    }

}

