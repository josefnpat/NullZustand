using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace NullZustand.MessageHandlers
{
    public abstract class MessageHandler : IMessageHandler
    {
        public abstract string MessageType { get; }
        public abstract Task HandleAsync(Message message, NetworkStream stream);

        protected T GetPayload<T>(Message message)
        {
            if (message.Payload == null)
                return default(T);

            if (message.Payload is JObject jObject)
            {
                return jObject.ToObject<T>();
            }

            if (message.Payload is JToken jToken)
            {
                return jToken.ToObject<T>();
            }

            if (message.Payload is T typedPayload)
            {
                return typedPayload;
            }

            string json = JsonConvert.SerializeObject(message.Payload);
            return JsonConvert.DeserializeObject<T>(json);
        }

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
