using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Newtonsoft.Json;
using System.Threading.Tasks;
using NullZustand;
using NullZustand.MessageHandlers;
using NullZustand.MessageHandlers.Handlers;

namespace NullZustand
{
    public class Program
    {
        static void Main(string[] args)
        {
            int port = ServerConstants.DEFAULT_PORT;

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
                                Console.WriteLine($"[INFO] Using default port: {ServerConstants.DEFAULT_PORT}");
                            }
                        }
                        else
                        {
                            Console.WriteLine($"[ERROR] {args[i]} flag requires a value");
                            Console.WriteLine($"[INFO] Using default port: {ServerConstants.DEFAULT_PORT}");
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
            Console.WriteLine($"  -p, --port <number>    Port number to listen on (default: {ServerConstants.DEFAULT_PORT})");
            Console.WriteLine("  -h, -help        Show this help message");
        }
    }


    public class Server
    {
        private const int BUFFER_SIZE = ServerConstants.BUFFER_SIZE;

        private TcpListener _listener;
        private MessageHandlerRegistry _handlerRegistry;

        public async Task StartAsync(int port)
        {
            try
            {
                // Initialize and register message handlers
                InitializeHandlers();

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

        private void InitializeHandlers()
        {
            _handlerRegistry = new MessageHandlerRegistry();

            // Register message handlers - easily comment out any handler to disable it
            _handlerRegistry.RegisterHandler(new PingMessageHandler());
            _handlerRegistry.RegisterHandler(new LoginRequestMessageHandler());

            // Example of how easy it is to add/remove handlers:
            // _handlerRegistry.RegisterHandler(new ExampleMessageHandler());

            // Add new handlers here as needed:
            // _handlerRegistry.RegisterHandler(new SomeOtherMessageHandler());
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

                bool handled = await _handlerRegistry.ProcessMessageAsync(message.Type, stream);
                if (!handled)
                {
                    Console.WriteLine($"[WARNING] No handler available for message type: {message.Type}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to process message: {ex.Message}");
            }
        }

    }
}