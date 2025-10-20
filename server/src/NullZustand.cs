using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using Newtonsoft.Json;
using System.Threading.Tasks;
using NullZustand;
using NullZustand.MessageHandlers;
using NullZustand.MessageHandlers.Handlers;

namespace NullZustand
{
    public class NullZustand
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
                    case "--help":
                        ShowHelp();
                        return;

                    default:
                        Console.WriteLine($"[ERROR] Unknown argument: {args[i]}");
                        Console.WriteLine("Use -h or --help for usage information");
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
            Console.WriteLine("Usage: NullZustand-Server.exe [options]");
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine($"  -p, --port <number>    Port number to listen on (default: {ServerConstants.DEFAULT_PORT})");
            Console.WriteLine("  -h, --help              Show this help message");
        }
    }


    public class Server
    {
        private const string SERVER_CERT_PATH = "build/server.pfx";
        
        private TcpListener _listener;
        private MessageHandlerRegistry _handlerRegistry;
        private SessionManager _sessionManager;
        private UserAccountManager _accountManager;
        private PlayerManager _playerManager;
        private X509Certificate2 _serverCertificate;

        public async Task StartAsync(int port)
        {
            try
            {
                // Load server certificate
                LoadCertificate();

                // Initialize managers and message handlers
                _playerManager = new PlayerManager();
                _sessionManager = new SessionManager(_playerManager);
                _accountManager = new UserAccountManager();
                InitializeHandlers();

                _listener = new TcpListener(IPAddress.Any, port);
                _listener.Start();
                Console.WriteLine($"[SERVER] Started on port {port} with SSL/TLS");
                Console.WriteLine($"[SERVER] Certificate: {_serverCertificate.Subject}");
                Console.WriteLine($"[SERVER] Idle session timeout: {ServerConstants.IDLE_SESSION_TIMEOUT_SECONDS}s");

                // Start background task for idle session cleanup
                _ = Task.Run(() => IdleSessionCleanupLoopAsync());

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

        private void LoadCertificate()
        {
            if (!File.Exists(SERVER_CERT_PATH))
            {
                throw new FileNotFoundException(
                    $"Server certificate not found at {SERVER_CERT_PATH}\n" +
                    "Run 'make' to generate certificates before starting the server.");
            }

            _serverCertificate = new X509Certificate2(SERVER_CERT_PATH, "");
            Console.WriteLine($"[CERT] Loaded server certificate from {SERVER_CERT_PATH}");
            Console.WriteLine($"[CERT] Thumbprint: {_serverCertificate.Thumbprint}");
        }

        private void InitializeHandlers()
        {
            _handlerRegistry = new MessageHandlerRegistry();

            // Register message handlers - easily comment out any handler to disable it
            _handlerRegistry.RegisterHandler(new PingMessageHandler());
            _handlerRegistry.RegisterHandler(new RegisterRequestMessageHandler(_accountManager));
            _handlerRegistry.RegisterHandler(new LoginRequestMessageHandler(_sessionManager, _accountManager, _playerManager));
            _handlerRegistry.RegisterHandler(new UpdatePositionMessageHandler(_playerManager));
            _handlerRegistry.RegisterHandler(new GetLocationUpdatesMessageHandler(_playerManager));
        }

        private async Task HandleClientAsync(TcpClient client)
        {
            SslStream sslStream = null;
            ClientSession session = null;

            try
            {
                // Set TCP socket timeouts to detect dead connections
                client.ReceiveTimeout = ServerConstants.SOCKET_READ_TIMEOUT_MS;
                client.SendTimeout = ServerConstants.SOCKET_WRITE_TIMEOUT_MS;

                NetworkStream networkStream = client.GetStream();
                networkStream.ReadTimeout = ServerConstants.SOCKET_READ_TIMEOUT_MS;
                networkStream.WriteTimeout = ServerConstants.SOCKET_WRITE_TIMEOUT_MS;

                sslStream = new SslStream(networkStream, false);

                // Authenticate as server (using TLS 1.2 - highest version available in .NET 4.5)
                await sslStream.AuthenticateAsServerAsync(_serverCertificate, false,
                    System.Security.Authentication.SslProtocols.Tls12,
                    true);

                session = _sessionManager.RegisterSession(client, sslStream);
                Console.WriteLine($"[CLIENT] Connected (SSL/TLS): {session}");

                while (client.Connected && sslStream.IsAuthenticated)
                {
                    string json = await MessageFraming.ReadMessageAsync(session.Stream);
                    if (json == null)
                    {
                        break;
                    }
                    session.UpdateActivity();
                    Message message = JsonConvert.DeserializeObject<Message>(json);

                    if (message != null)
                    {
                        await ProcessMessageAsync(message, session);
                    }
                    else
                    {
                        Console.WriteLine("[WARNING] Failed to deserialize message from JSON");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Client handling failed{(session != null ? $" for session {session.SessionId}" : "")}: {ex.Message}");
            }
            finally
            {
                if (session != null)
                {
                    Console.WriteLine($"[CLIENT] Disconnected: {session}");
                    _sessionManager.RemoveSession(session.SessionId);
                    _handlerRegistry.OnSessionDisconnect(session.SessionId);
                }

                try
                {
                    sslStream?.Close();
                    client.Close();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ERROR] Failed to close connection: {ex.Message}");
                }
            }
        }

        private async Task ProcessMessageAsync(Message message, ClientSession session)
        {
            try
            {
                Console.WriteLine($"[MESSAGE] Received from {session.SessionId}: {message.Type}");

                bool handled = await _handlerRegistry.ProcessMessageAsync(message, session);
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

        private async Task IdleSessionCleanupLoopAsync()
        {
            Console.WriteLine($"[SERVER] Started idle session cleanup task (interval: {ServerConstants.IDLE_CLEANUP_INTERVAL_SECONDS}s)");

            while (true)
            {
                try
                {
                    await Task.Delay(ServerConstants.IDLE_CLEANUP_INTERVAL_SECONDS * 1000);

                    int cleaned = _sessionManager.CleanupIdleSessions(ServerConstants.IDLE_SESSION_TIMEOUT_SECONDS);
                    if (cleaned > 0)
                    {
                        Console.WriteLine($"[SERVER] Cleaned up {cleaned} idle session(s)");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ERROR] Idle session cleanup failed: {ex.Message}");
                }
            }
        }

    }
}