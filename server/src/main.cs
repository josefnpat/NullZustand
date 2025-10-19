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
            Server server = new Server();
            try
            {
                server.StartAsync(7777).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Server error: {ex.Message}");
            }
        }
    }
    
    public class Server
    {
        private TcpListener _listener;

        public async Task StartAsync(int port = 7777)
        {
            try
            {
                _listener = new TcpListener(IPAddress.Any, port);
                _listener.Start();
                Console.WriteLine($"Server started on port {port}");

                while (true)
                {
                    TcpClient client = await _listener.AcceptTcpClientAsync();
                    _ = HandleClientAsync(client);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Server startup error: {ex.Message}");
                throw;
            }
        }

        private async Task HandleClientAsync(TcpClient client)
        {
            Console.WriteLine("Client connected.");
            NetworkStream stream = client.GetStream();
            byte[] buffer = new byte[4096];

            try
            {
                while (client.Connected)
                {
                    int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                    if (bytesRead == 0) break;

                    string json = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    Message message = JsonConvert.DeserializeObject<Message>(json);

                    await ProcessMessageAsync(message, stream);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling client: {ex.Message}");
            }
            finally
            {
                Console.WriteLine("Client disconnected.");
                try
                {
                    client.Close();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error closing client: {ex.Message}");
                }
            }
        }

        private async Task ProcessMessageAsync(Message message, NetworkStream stream)
        {
            try
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
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing message: {ex.Message}");
            }
        }

        private async Task SendAsync(NetworkStream stream, Message message)
        {
            try
            {
                string json = JsonConvert.SerializeObject(message);
                byte[] bytes = Encoding.UTF8.GetBytes(json);
                await stream.WriteAsync(bytes, 0, bytes.Length);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending message: {ex.Message}");
            }
        }

        public class Message
        {
            public string Type { get; set; }
            public object Payload { get; set; }
        }
    }
}