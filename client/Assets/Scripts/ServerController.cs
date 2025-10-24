using System;
using System.IO;
using System.Net.Sockets;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;
using NullZustand;
using System.Collections.Generic;
using ClientMessageHandlers;
using ClientMessageHandlers.Handlers;

public class ConnectionResult
{
    public bool Success { get; set; }
    public string ErrorMessage { get; set; }
}

[System.Serializable]
public class ServerInfo
{
    public string name;
    public string host;
    public int port;
}

[System.Serializable]
public class ServerListResponse
{
    public List<ServerInfo> servers;
}

public class ServerController : MonoBehaviour
{
    private const string SERVER_LIST_URL = "https://gist.githubusercontent.com/josefnpat/11ed0e82e8068dc67697ec04dd97734b/raw/48b97c06e7922fe8017b0c3f2b92b12b8b28286f/servers.json";

    private TcpClient _client;
    private SslStream _stream;
    private X509Certificate2 _pinnedCertificate;
    private long _lastLocationUpdateId = 0;
    private long _serverClockOffset = 0;
    private ClientMessageHandlerRegistry _handlerRegistry;
    private ResponseCallbacks _responseCallbacks = new ResponseCallbacks();
    private float _lastCallbackCleanupTime = 0f;
    private const float CALLBACK_CLEANUP_INTERVAL = 5f;
    private float _lastTimeSyncTime = 0f;
    private const float TIME_SYNC_INTERVAL = 60f;
    private Task<ConnectionResult> _connectionTask = null;
    private string _lastConnectionError = null;
    private SemaphoreSlim _writeLock = new SemaphoreSlim(1, 1);
    private List<ServerInfo> _serverList = new List<ServerInfo>();
    private ServerInfo _currentServer;
    private bool _isAuthenticated = false;
    private Player _currentPlayer = null;

    public event Action<Player> OnPlayerUpdate;
    public event Action<string, string> OnError; // (errorCode, errorMessage)
    public event Action OnSessionDisconnect;
    public event Action OnPlayerAuthenticate;
    public event Action<string, string, long> OnNewChatMessage; // (username, message, timestamp)
    public event Action OnTimeSyncUpdate;
    public event Action OnServerListUpdate;

    void Awake()
    {
        ServiceLocator.Register<ServerController>(this);
        InitializeHandlers();
    }

    void Start()
    {
        LoadPinnedCertificate();
        // Subscribe to own authentication event to trigger time sync
        OnPlayerAuthenticate += OnPlayerAuthenticated;
        FetchServerList();
    }

    void Update()
    {
        // Periodically clean up expired callbacks to prevent memory leaks
        if (Time.unscaledTime - _lastCallbackCleanupTime > CALLBACK_CLEANUP_INTERVAL)
        {
            _lastCallbackCleanupTime = Time.unscaledTime;
            int cleanedUp = _responseCallbacks.CleanupExpiredCallbacks();

            if (cleanedUp > 0)
            {
                Debug.LogWarning($"[ServerController] Cleaned up {cleanedUp} expired callback(s)");
            }
        }

        // Periodically sync time with server while connected
        if (IsConnected())
        {
            if (Time.unscaledTime - _lastTimeSyncTime > TIME_SYNC_INTERVAL)
            {
                _lastTimeSyncTime = Time.unscaledTime;
                SyncTime(OnSyncTimeSuccess, OnSyncTimeFailure);
            }
        }
    }

    private void InitializeHandlers()
    {
        _handlerRegistry = new ClientMessageHandlerRegistry();
        // Register message handlers - easily comment out any handler to disable it
        _handlerRegistry.RegisterHandler(new ChatMessageHandler());
        _handlerRegistry.RegisterHandler(new ErrorMessageHandler());
        _handlerRegistry.RegisterHandler(new LocationUpdatesMessageHandler());
        _handlerRegistry.RegisterHandler(new LoginHandlerMessageHandler());
        _handlerRegistry.RegisterHandler(new PingMessageHandler());
        _handlerRegistry.RegisterHandler(new RegisterMessageHandler());
        _handlerRegistry.RegisterHandler(new ResyncRequiredMessageHandler());
        _handlerRegistry.RegisterHandler(new TimeSyncMessageHandler());
        _handlerRegistry.RegisterHandler(new UpdatePositionMessageHandler());
    }

    public void FetchServerList()
    {
        if (string.IsNullOrEmpty(SERVER_LIST_URL))
        {
            SetDefaultServerList();
        }
        else
        {
            StartCoroutine(FetchServerListCoroutine());
        }
    }

    private System.Collections.IEnumerator FetchServerListCoroutine()
    {
        using (UnityWebRequest request = UnityWebRequest.Get(SERVER_LIST_URL))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    string jsonResponse = request.downloadHandler.text;
                    ServerListResponse response = JsonConvert.DeserializeObject<ServerListResponse>(jsonResponse);
                    if (response != null && response.servers != null && response.servers.Count > 0)
                    {
                        _serverList = response.servers;
                        _currentServer = _serverList[0];
                        OnServerListUpdate?.Invoke();
                    }
                    else
                    {
                        Debug.LogWarning("Server list was empty or invalid, using default");
                        SetDefaultServerList();
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Failed to parse server list JSON: {ex.Message}");
                    SetDefaultServerList();
                }
            }
            else
            {
                Debug.LogWarning($"Failed to fetch server list: {request.error}");
                SetDefaultServerList();
            }
        }
    }

    private void SetDefaultServerList()
    {
        _serverList = new List<ServerInfo>
        {
            new ServerInfo
            {
                name = "localhost",
                host = "127.0.0.1",
                port = ServerConstants.DEFAULT_PORT
            }
        };
        _currentServer = _serverList[0];
        OnServerListUpdate?.Invoke();
    }

    public List<ServerInfo> GetServerList()
    {
        return _serverList;
    }

    public ServerInfo GetCurrentServer()
    {
        return _currentServer;
    }

    public void SetCurrentServer(ServerInfo server)
    {
        if (IsConnected())
        {
            Disconnect();
        }
        _currentServer = server;
    }

    public async void Register(string username, string password, Action<object> onSuccess = null, Action<string> onFailure = null)
    {
        var result = await EnsureConnectedAsync();
        if (!result.Success)
        {
            onFailure?.Invoke(result.ErrorMessage);
            return;
        }

        var handler = _handlerRegistry.GetHandler<IClientMessageHandler<string, string>>(MessageTypes.REGISTER_REQUEST);
        if (handler != null)
            await handler.SendRequestAsync(this, username, password, onSuccess, onFailure);
    }

    public async void Login(string username, string password, Action<object> onSuccess = null, Action<string> onFailure = null)
    {
        var result = await EnsureConnectedAsync();
        if (!result.Success)
        {
            onFailure?.Invoke(result.ErrorMessage);
            return;
        }

        _currentPlayer = new Player(username);

        var handler = _handlerRegistry.GetHandler<IClientMessageHandler<string, string>>(MessageTypes.LOGIN_REQUEST);
        if (handler != null)
            await handler.SendRequestAsync(this, username, password, onSuccess, onFailure);
    }

    public async void SendPing(Action<object> onSuccess = null, Action<string> onFailure = null)
    {
        var result = await EnsureConnectedAsync();
        if (!result.Success)
        {
            onFailure?.Invoke(result.ErrorMessage);
            return;
        }

        var handler = _handlerRegistry.GetHandler<IClientMessageHandlerNoParam>(MessageTypes.PING);
        if (handler != null)
            await handler.SendRequestAsync(this, onSuccess, onFailure);
    }

    public async void SyncTime(Action<object> onSuccess = null, Action<string> onFailure = null)
    {
        var result = await EnsureConnectedAsync();
        if (!result.Success)
        {
            onFailure?.Invoke(result.ErrorMessage);
            return;
        }

        var handler = _handlerRegistry.GetHandler<IClientMessageHandlerNoParam>(MessageTypes.TIME_SYNC_REQUEST);
        if (handler != null)
            await handler.SendRequestAsync(this, onSuccess, onFailure);
    }

    public async void UpdatePosition(Quaternion rotation, float velocity, Action<object> onSuccess = null, Action<string> onFailure = null)
    {
        var handler = _handlerRegistry.GetHandler<IClientMessageHandler<Quaternion, float>>(MessageTypes.UPDATE_POSITION_REQUEST);
        if (handler != null)
            await handler.SendRequestAsync(this, rotation, velocity, onSuccess, onFailure);
    }

    public async void GetLocationUpdates(Action<object> onSuccess = null, Action<string> onFailure = null)
    {
        var handler = _handlerRegistry.GetHandler<IClientMessageHandlerNoParam>(MessageTypes.LOCATION_UPDATES_REQUEST);
        if (handler != null)
            await handler.SendRequestAsync(this, onSuccess, onFailure);
    }

    public async void SendNewChatMessage(string message, Action<object> onSuccess = null, Action<string> onFailure = null)
    {
        var handler = _handlerRegistry.GetHandler<IClientMessageHandler<string>>(MessageTypes.CHAT_MESSAGE_REQUEST);
        if (handler != null)
            await handler.SendRequestAsync(this, message, onSuccess, onFailure);
    }

    public void Disconnect()
    {
        if (_client != null || _stream != null)
        {
            Debug.Log("Disconnecting from server...");
            Cleanup();
        }
        else
        {
            Debug.Log("Not connected to server.");
        }
    }

    public bool IsConnected()
    {
        return _client != null && _client.Connected && _stream != null;
    }

    public bool IsAuthenticated()
    {
        return _isAuthenticated;
    }

    public Player GetCurrentPlayer()
    {
        return _currentPlayer;
    }

    private bool IsClientDisconnected()
    {
        return _client != null && !_client.Connected;
    }

    private void LoadPinnedCertificate()
    {
        try
        {
            // Path to the pinned certificate - using StreamingAssets for both editor and builds
            string certPath = Path.Combine(Application.streamingAssetsPath, "server.cer");

            if (!File.Exists(certPath))
            {
                Debug.LogError($"Pinned certificate not found at: {certPath}");
                Debug.LogError("Run the server build first to generate the certificate.");
                return;
            }

            _pinnedCertificate = new X509Certificate2(certPath);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to load pinned certificate: {ex.Message}");
        }
    }

    private async Task<ConnectionResult> EnsureConnectedAsync()
    {
        // Already connected
        if (IsConnected())
        {
            return new ConnectionResult { Success = true };
        }

        // Clean up any stale connection state
        if (IsClientDisconnected())
        {
            CleanupFailedConnection();
        }

        // Connection already in progress - wait for it
        if (_connectionTask != null && !_connectionTask.IsCompleted)
        {
            try
            {
                return await _connectionTask;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Connection attempt failed: {ex.Message}");
                _connectionTask = null;
                return new ConnectionResult
                {
                    Success = false,
                    ErrorMessage = _lastConnectionError ?? "Failed to connect to server"
                };
            }
        }

        // Start new connection
        if (_currentServer == null)
        {
            _lastConnectionError = "No server selected";
            return new ConnectionResult
            {
                Success = false,
                ErrorMessage = _lastConnectionError
            };
        }

        _connectionTask = ConnectToServerAsync(_currentServer.host, _currentServer.port);
        try
        {
            return await _connectionTask;
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"Connection attempt failed: {ex.Message}");
            _connectionTask = null;
            return new ConnectionResult
            {
                Success = false,
                ErrorMessage = _lastConnectionError ?? "Failed to connect to server"
            };
        }
    }

    private async Task<ConnectionResult> ConnectToServerAsync(string host, int port)
    {
        // Reset connection error at the start of a new connection attempt
        _lastConnectionError = null;

        try
        {
            if (_pinnedCertificate == null)
            {
                Debug.LogError("Cannot connect: Pinned certificate not loaded");
                _lastConnectionError = "Pinned certificate not loaded";
                throw new InvalidOperationException(_lastConnectionError);
            }

            // Connect to server
            _client = new TcpClient();
            await _client.ConnectAsync(host, port);

            // Create SSL stream with certificate validation
            NetworkStream networkStream = _client.GetStream();
            _stream = new SslStream(
                networkStream,
                false,
                ValidateServerCertificate,
                null
            );

            // Authenticate as client
            await _stream.AuthenticateAsClientAsync(host);

            _ = ListenForMessagesAsync();

            Debug.Log("Connected to server successfully");
            return new ConnectionResult { Success = true };
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to connect to server: {ex.Message}");
            CleanupFailedConnection();

            // Return the specific error if we have one, otherwise generic message
            return new ConnectionResult
            {
                Success = false,
                ErrorMessage = _lastConnectionError ?? "Failed to connect to server"
            };
        }
    }

    private void CleanupFailedConnection()
    {
        try
        {
            _stream?.Close();
            _client?.Close();
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"Error during failed connection cleanup: {ex.Message}");
        }
        finally
        {
            _stream = null;
            _client = null;
            _connectionTask = null;
            _isAuthenticated = false;
            _currentPlayer = null;
        }
    }

    private bool ValidateServerCertificate(
        object sender,
        X509Certificate certificate,
        X509Chain chain,
        SslPolicyErrors sslPolicyErrors)
    {
        // For self-signed certificates, we expect RemoteCertificateChainErrors
        if (sslPolicyErrors == SslPolicyErrors.None)
        {
            return true;
        }

        // Check if the certificate matches our pinned certificate
        if (certificate == null || _pinnedCertificate == null)
        {
            Debug.LogError("[SSL] Certificate validation failed: Missing certificate");
            _lastConnectionError = "Cert doesn't match. Please update client.";
            return false;
        }

        // Convert to X509Certificate2 to access Thumbprint property
        X509Certificate2 serverCert = new X509Certificate2(certificate);

        // Compare thumbprints (this is the "pinning" part)
        string serverThumbprint = serverCert.Thumbprint;
        string pinnedThumbprint = _pinnedCertificate.Thumbprint;

        bool matches = serverThumbprint == pinnedThumbprint;

        if (!matches)
        {
            Debug.LogError($"[SSL] Certificate pinning validation FAILED");
            Debug.LogError($"[SSL] Server thumbprint: {serverThumbprint}");
            Debug.LogError($"[SSL] Pinned thumbprint: {pinnedThumbprint}");
            _lastConnectionError = "Cert doesn't match. Please update client.";
        }

        return matches;
    }

    private async Task ListenForMessagesAsync()
    {
        try
        {
            while (IsConnected())
            {
                string json = await MessageFraming.ReadMessageAsync(_stream);
                if (json == null)
                {
                    Debug.LogWarning("Connection closed or read failed");
                    break;
                }

                Message message = JsonConvert.DeserializeObject<Message>(json);
                if (message != null)
                {
                    HandleMessage(message);
                }
                else
                {
                    Debug.LogWarning("Failed to deserialize message from JSON");
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error listening for messages: {ex.Message}");
        }
        finally
        {
            Debug.Log("Disconnected from server.");
            Cleanup();
        }
    }

    private void HandleMessage(Message message)
    {
        Debug.Log($"Received: {message.Type} | Payload: {JsonConvert.SerializeObject(message.Payload)}");

        var entityManager = ServiceLocator.Get<EntityManager>();
        var playerManager = ServiceLocator.Get<PlayerManager>();
        var context = new MessageHandlerContext(this, entityManager, playerManager);
        bool handled = _handlerRegistry.ProcessMessage(message, context);
        if (!handled)
        {
            Debug.LogWarning($"[CLIENT] No handler for message type: {message.Type}");
        }
    }

    // Public methods for handlers to use
    public void SetLastLocationUpdateId(long updateId)
    {
        _lastLocationUpdateId = updateId;
    }

    public long GetLastLocationUpdateId()
    {
        return _lastLocationUpdateId;
    }


    public void RegisterResponseCallbacks(string messageId, Action<object> onSuccess, Action<string> onFailure)
    {
        _responseCallbacks.RegisterCallbacks(messageId, onSuccess, onFailure);
    }

    public void InvokeResponseSuccess(string messageId, object payload)
    {
        _responseCallbacks.InvokeSuccess(messageId, payload);
    }

    public void InvokeResponseFailure(string messageId, string error)
    {
        _responseCallbacks.InvokeFailure(messageId, error);
    }

    public void InvokeError(string code, string message)
    {
        OnError?.Invoke(code, message);
    }

    public void TriggerPlayerUpdate(Player player)
    {
        OnPlayerUpdate?.Invoke(player);
    }

    public void InvokePlayerAuthenticate()
    {
        _isAuthenticated = true;
        OnPlayerAuthenticate?.Invoke();
    }

    public void InvokeNewChatMessage(string username, string message, long timestamp)
    {
        OnNewChatMessage?.Invoke(username, message, timestamp);
    }

    private void OnPlayerAuthenticated()
    {
        // Automatically sync time with server after successful login
        SyncTime(OnSyncTimeSuccess, OnSyncTimeFailure);
    }

    private void OnSyncTimeSuccess(object payload)
    {
        Debug.Log("[ServerController] Time synchronized.");
    }

    private void OnSyncTimeFailure(string error)
    {
        Debug.LogWarning($"[ServerController] Failed to sync time: {error}");
    }

    public void SetServerClockOffset(long offsetMs)
    {
        _serverClockOffset = offsetMs;
        long serverTime = GetServerTime();
        Debug.Log($"[ServerController] Server clock offset set to: {offsetMs}ms, Server time: {serverTime}ms");

        _lastTimeSyncTime = Time.unscaledTime;

        OnTimeSyncUpdate?.Invoke();
    }

    public long GetServerTime()
    {
        long localTime = NullZustand.TimeUtils.GetUnixTimestampMs();
        return localTime + _serverClockOffset;
    }

    public async Task SendMessageAsync(Message message)
    {
        try
        {
            if (!IsConnected())
            {
                InvokeError("NOT_CONNECTED", "Cannot send message: not connected to server");
                return;
            }

            string json = JsonConvert.SerializeObject(message);

            // Use semaphore to ensure only one write operation at a time
            await _writeLock.WaitAsync();
            try
            {
                await MessageFraming.WriteMessageAsync(_stream, json);
            }
            finally
            {
                _writeLock.Release();
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to send message: {ex.Message}");
        }
    }

    private void Cleanup()
    {
        bool wasConnected = _client != null && _stream != null;

        try
        {
            _stream?.Close();
            _client?.Close();
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error during cleanup: {ex.Message}");
        }
        finally
        {
            _stream = null;
            _client = null;
            _connectionTask = null;
            _lastLocationUpdateId = 0;
            _serverClockOffset = 0;
            _lastTimeSyncTime = 0f;
            _lastConnectionError = null;
            _isAuthenticated = false;
            _currentPlayer = null;
            _responseCallbacks.CleanupExpiredCallbacks(0f);

            // Notify subscribers that session has disconnected
            if (wasConnected)
            {
                OnSessionDisconnect?.Invoke();
            }
        }
    }

    void OnDestroy()
    {
        OnPlayerAuthenticate -= OnPlayerAuthenticated;
        Cleanup();
        try
        {
            _writeLock?.Dispose();
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error disposing write lock: {ex.Message}");
        }
    }

}
