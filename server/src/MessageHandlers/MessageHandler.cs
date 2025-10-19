using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace NullZustand.MessageHandlers
{
    public abstract class MessageHandler : IMessageHandler
    {
        public abstract string MessageType { get; }
        public virtual bool RequiresAuthentication => false;
        public abstract Task HandleAsync(Message message, ClientSession session);

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

        protected async Task SendAsync(ClientSession session, Message message)
        {
            try
            {
                string json = JsonConvert.SerializeObject(message);
                await MessageFraming.WriteMessageAsync(session.Stream, json);
                Console.WriteLine($"[MESSAGE] Sent to {session.SessionId}: {message.Type}");
            }
            catch (System.Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to send message to {session.SessionId}: {ex.Message}");
            }
        }

        protected async Task SendResponseAsync(ClientSession session, Message requestMessage, string responseType, object payload)
        {
            await SendAsync(session, new Message
            {
                Id = requestMessage.Id,
                Type = responseType,
                Payload = payload
            });
        }
    }
}
