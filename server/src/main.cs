using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Newtonsoft.Json;
using System.Threading.Tasks;

namespace NullZustand
{

    public class Program
    {
        static void Main(string[] args)
        {
            var server = new Server();
            server.StartAsync(7777).Wait();
        }
    }
    
    public class Server
    {
        private TcpListener _listener;

        public async Task StartAsync(int port = 7777)
        {
            _listener = new TcpListener(IPAddress.Any, port);
            _listener.Start();
            Console.WriteLine($"Server started on port {port}");

            while (true)
            {
                var client = await _listener.AcceptTcpClientAsync();
                _ = HandleClientAsync(client);
            }
        }

        private async Task HandleClientAsync(TcpClient client)
        {
            Console.WriteLine("Client connected.");
            var stream = client.GetStream();
            var buffer = new byte[4096];

            while (client.Connected)
            {
                int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                if (bytesRead == 0) break;

                var json = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                var message = JsonConvert.DeserializeObject<Message>(json);

                await ProcessMessageAsync(message, stream);
            }

            Console.WriteLine("Client disconnected.");
            client.Close();
        }

        private async Task ProcessMessageAsync(Message message, NetworkStream stream)
        {
            Console.WriteLine($"Received: {message.Type}");

            switch (message.Type)
            {
                case "Ping":
                    await SendAsync(stream, new Message { Type = "Pong", Payload = new { time = DateTime.UtcNow } });
                    break;

                case "LoginRequest":
                    await SendAsync(stream, new Message
                    {
                        Type = "LoginResponse",
                        Payload = new { success = true, sessionToken = "abc123" }
                    });
                    break;

                default:
                    Console.WriteLine($"Unknown message type: {message.Type}");
                    break;
            }
        }

        private async Task SendAsync(NetworkStream stream, Message message)
        {
            var json = JsonConvert.SerializeObject(message);
            var bytes = Encoding.UTF8.GetBytes(json);
            await stream.WriteAsync(bytes, 0, bytes.Length);
        }

        public class Message
        {
            public string Type { get; set; }
            public object Payload { get; set; }
        }
    }
}