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
        private const int DEFAULT_PORT = 8140;

        static void Main(string[] args)
        {
            int port = DEFAULT_PORT;

            // Parse command line arguments
            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i].ToLower())
                {
                    case "-p":
                    case "--port":
                        if (i + 1 < args.Length)
                        {
                            if (int.TryParse(args[i + 1], out int parsedPort))
                            {
                                port = parsedPort;
                                i++; // Skip the next argument since we consumed it
                            }
                            else
                            {
                                Console.WriteLine($"[ERROR] Invalid port number: {args[i + 1]}");
                                Console.WriteLine($"[INFO] Using default port: {DEFAULT_PORT}");
                            }
                        }
                        else
                        {
                            Console.WriteLine($"[ERROR] {args[i]} flag requires a value");
                            Console.WriteLine($"[INFO] Using default port: {DEFAULT_PORT}");
                        }
                        break;

                    case "-h":
                    case "-help":
                        ShowHelp();
                        return;

                    default:
                        Console.WriteLine($"[ERROR] Unknown argument: {args[i]}");
                        Console.WriteLine("Use -h or -help for usage information");
                        break;
                }
            }

            Console.WriteLine($"[INFO] Starting server on port: {port}");

            Server server = new Server();
            try
            {
                server.StartAsync(port).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Server error: {ex.Message}");
            }
        }

        private static void ShowHelp()
        {
            Console.WriteLine("NullZustand Server");
            Console.WriteLine("Usage: NullZustand.exe [options]");
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine($"  -p, --port <number>    Port number to listen on (default: {DEFAULT_PORT})");
            Console.WriteLine("  -h, -help        Show this help message");
        }
    }

    public class Message
    {
        public string Type { get; set; }
        public object Payload { get; set; }
    }

    public class Server
    {
        private const int BUFFER_SIZE = 4096;
        private const string PING_MESSAGE_TYPE = "Ping";
        private const string PONG_MESSAGE_TYPE = "Pong";
        private const string LOGIN_REQUEST_TYPE = "LoginRequest";
        private const string LOGIN_RESPONSE_TYPE = "LoginResponse";

        private TcpListener _listener;

        public async Task StartAsync(int port)
        {
            try
            {
                _listener = new TcpListener(IPAddress.Any, port);
                _listener.Start();
                Console.WriteLine($"[SERVER] Started on port {port}");

                while (true)
                {
                    TcpClient client = await _listener.AcceptTcpClientAsync();
                    _ = Task.Run(() => HandleClientAsync(client));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Server startup failed: {ex.Message}");
                throw;
            }
        }

        private async Task HandleClientAsync(TcpClient client)
        {
            Console.WriteLine("[CLIENT] Connected");
            NetworkStream stream = client.GetStream();
            byte[] buffer = new byte[BUFFER_SIZE];

            try
            {
                while (client.Connected)
                {
                    int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                    if (bytesRead == 0) break;

                    string json = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    Message message = JsonConvert.DeserializeObject<Message>(json);

                    if (message != null)
                    {
                        await ProcessMessageAsync(message, stream);
                    }
                    else
                    {
                        Console.WriteLine("[WARNING] Failed to deserialize message from JSON");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Client handling failed: {ex.Message}");
            }
            finally
            {
                Console.WriteLine("[CLIENT] Disconnected");
                try
                {
                    client.Close();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ERROR] Failed to close client: {ex.Message}");
                }
            }
        }

        private async Task ProcessMessageAsync(Message message, NetworkStream stream)
        {
            try
            {
                Console.WriteLine($"[MESSAGE] Received: {message.Type}");

                switch (message.Type)
                {
                    case PING_MESSAGE_TYPE:
                        await HandlePingMessageAsync(stream);
                        break;

                    case LOGIN_REQUEST_TYPE:
                        await HandleLoginRequestAsync(stream);
                        break;

                    default:
                        Console.WriteLine($"[WARNING] Unknown message type: {message.Type}");
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to process message: {ex.Message}");
            }
        }

        private async Task HandlePingMessageAsync(NetworkStream stream)
        {
            Message response = new Message
            {
                Type = PONG_MESSAGE_TYPE,
                Payload = new { time = DateTime.UtcNow }
            };
            await SendAsync(stream, response);
        }

        private async Task HandleLoginRequestAsync(NetworkStream stream)
        {
            Message response = new Message
            {
                Type = LOGIN_RESPONSE_TYPE,
                Payload = new { success = true, sessionToken = "abc123" }
            };
            await SendAsync(stream, response);
        }

        private async Task SendAsync(NetworkStream stream, Message message)
        {
            try
            {
                string json = JsonConvert.SerializeObject(message);
                byte[] bytes = Encoding.UTF8.GetBytes(json);
                await stream.WriteAsync(bytes, 0, bytes.Length);
                Console.WriteLine($"[MESSAGE] Sent: {message.Type}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to send message: {ex.Message}");
            }
        }
    }
}