using System;
using System.Threading.Tasks;
using NullZustand;

namespace ClientMessageHandlers
{
    public interface IClientMessageHandler
    {
        string RequestMessageType { get; }
        string ResponseMessageType { get; }
        void HandleResponse(Message message, ServerController serverController);
    }

    public interface IClientMessageHandlerNoParam : IClientMessageHandler
    {
        Task<string> SendRequestAsync(ServerController serverController, Action<object> onSuccess = null, Action<string> onFailure = null);
    }

    public interface IClientMessageHandler<in T> : IClientMessageHandler
    {
        Task<string> SendRequestAsync(ServerController serverController, T data, Action<object> onSuccess = null, Action<string> onFailure = null);
    }

    public interface IClientMessageHandler<in T1, in T2> : IClientMessageHandler
    {
        Task<string> SendRequestAsync(ServerController serverController, T1 data1, T2 data2, Action<object> onSuccess = null, Action<string> onFailure = null);
    }

    public interface IClientMessageHandler<in T1, in T2, in T3, in T4> : IClientMessageHandler
    {
        Task<string> SendRequestAsync(ServerController serverController, T1 data1, T2 data2, T3 data3, T4 data4, Action<object> onSuccess = null, Action<string> onFailure = null);
    }


}

