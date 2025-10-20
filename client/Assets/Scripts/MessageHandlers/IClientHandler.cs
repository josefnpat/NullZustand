using System;
using System.Threading.Tasks;
using NullZustand;

namespace ClientMessageHandlers
{
    public interface IClientHandler
    {
        string RequestMessageType { get; }
        string ResponseMessageType { get; }
        void HandleResponse(Message message, ServerController serverController);
    }

    public interface IClientHandler<in T> : IClientHandler
    {
        Task<string> SendRequestAsync(ServerController serverController, T data, Action<object> onSuccess = null, Action<string> onFailure = null);
    }

    public interface IClientHandler<in T1, in T2> : IClientHandler
    {
        Task<string> SendRequestAsync(ServerController serverController, T1 data1, T2 data2, Action<object> onSuccess = null, Action<string> onFailure = null);
    }

    public interface IClientHandler<in T1, in T2, in T3> : IClientHandler
    {
        Task<string> SendRequestAsync(ServerController serverController, T1 data1, T2 data2, T3 data3, Action<object> onSuccess = null, Action<string> onFailure = null);
    }

    public interface IClientHandler<in T1, in T2, in T3, in T4, in T5, in T6, in T7> : IClientHandler
    {
        Task<string> SendRequestAsync(ServerController serverController, T1 data1, T2 data2, T3 data3, T4 data4, T5 data5, T6 data6, T7 data7, Action<object> onSuccess = null, Action<string> onFailure = null);
    }

    // For parameterless requests
    public interface IClientHandlerNoParam : IClientHandler
    {
        Task<string> SendRequestAsync(ServerController serverController, Action<object> onSuccess = null, Action<string> onFailure = null);
    }
}

