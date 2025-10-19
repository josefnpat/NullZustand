using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace NullZustand.MessageHandlers
{
    public abstract class MessageHandler : IMessageHandler
    {
        public abstract string MessageType { get; }
        public abstract Task HandleAsync(NetworkStream stream);

        protected async Task SendAsync(NetworkStream stream, Message message)
        {
            try
            {
                string json = JsonConvert.SerializeObject(message);
                await MessageFraming.WriteMessageAsync(stream, json);
                Console.WriteLine($"[MESSAGE] Sent: {message.Type}");
            }
            catch (System.Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to send message: {ex.Message}");
            }
        }
    }
}
