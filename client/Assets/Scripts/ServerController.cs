using System;
using System.IO;
using System.Net.Sockets;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using UnityEngine;
using Newtonsoft.Json;
using NullZustand;
using System.Collections.Generic;
using ClientMessageHandlers;
using ClientMessageHandlers.Handlers;

public class ServerController : MonoBehaviour
{
    [SerializeField]
    private string serverHost = "127.0.0.1";

    private TcpClient _client;
    private SslStream _stream;
    private X509Certificate2 _pinnedCertificate;
    private long _lastLocationUpdateId = 0;
    private Dictionary<string, Vector3> _playerPositions = new Dictionary<string, Vector3>();
    private Dictionary<string, Quaternion> _playerRotations = new Dictionary<string, Quaternion>();
    private ClientMessageHandlerRegistry _handlerRegistry;
    private ResponseCallbacks _responseCallbacks = new ResponseCallbacks();
    private float _lastCallbackCleanupTime = 0f;
    private const float CALLBACK_CLEANUP_INTERVAL = 5f;
    private Task _connectionTask = null;

    public event Action<string, Vector3, Quaternion> OnLocationUpdate;
    public event Action<string, string> OnError; // (errorCode, errorMessage)
    public event Action OnSessionDisconnect;
    public event Action OnPlayerAuthenticate;

    void Awake()
    {
        ServiceLocator.Register<ServerController>(this);
        InitializeHandlers();
    }

    void Start()
    {
        LoadPinnedCertificate();
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
    }

    private void InitializeHandlers()
    {
        _handlerRegistry = new ClientMessageHandlerRegistry();

        // Register message handlers - easily comment out any handler to disable it
        _handlerRegistry.RegisterHandler(new RegisterHandler());
        _handlerRegistry.RegisterHandler(new LoginHandler());
        _handlerRegistry.RegisterHandler(new PingHandler());
        _handlerRegistry.RegisterHandler(new UpdatePositionHandler());
        _handlerRegistry.RegisterHandler(new LocationUpdatesHandler());
        _handlerRegistry.RegisterHandler(new ErrorHandler());
    }

    public async void Register(string username, string password, Action<object> onSuccess = null, Action<string> onFailure = null)
    {
        if (!await EnsureConnectedAsync())
        {
            onFailure?.Invoke("Failed to connect to server");
            return;
        }

        var handler = _handlerRegistry.GetHandler<IClientHandler<string, string>>(MessageTypes.REGISTER_REQUEST);
        if (handler != null)
            await handler.SendRequestAsync(this, username, password, onSuccess, onFailure);
    }

    public async void Login(string username, string password, Action<object> onSuccess = null, Action<string> onFailure = null)
    {
        if (!await EnsureConnectedAsync())
        {
            onFailure?.Invoke("Failed to connect to server");
            return;
        }

        var handler = _handlerRegistry.GetHandler<IClientHandler<string, string>>(MessageTypes.LOGIN_REQUEST);
        if (handler != null)
            await handler.SendRequestAsync(this, username, password, onSuccess, onFailure);
    }

    public async void SendPing(Action<object> onSuccess = null, Action<string> onFailure = null)
    {
        if (!await EnsureConnectedAsync())
        {
            onFailure?.Invoke("Failed to connect to server");
            return;
        }

        var handler = _handlerRegistry.GetHandler<IClientHandlerNoParam>(MessageTypes.PING);
        if (handler != null)
            await handler.SendRequestAsync(this, onSuccess, onFailure);
    }

    public async void UpdatePosition(float x, float y, float z, float rotX, float rotY, float rotZ, float rotW, Action<object> onSuccess = null, Action<string> onFailure = null)
    {
        var handler = _handlerRegistry.GetHandler<IClientHandler<float, float, float, float, float, float, float>>(MessageTypes.UPDATE_POSITION_REQUEST);
        if (handler != null)
            await handler.SendRequestAsync(this, x, y, z, rotX, rotY, rotZ, rotW, onSuccess, onFailure);
    }

    public async void GetLocationUpdates(Action<object> onSuccess = null, Action<string> onFailure = null)
    {
        var handler = _handlerRegistry.GetHandler<IClientHandlerNoParam>(MessageTypes.LOCATION_UPDATES_REQUEST);
        if (handler != null)
            await handler.SendRequestAsync(this, onSuccess, onFailure);
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

    private void LoadPinnedCertificate()
    {
        try
        {
            // Path to the pinned certificate in the Unity project
            string certPath = Path.Combine(Application.dataPath, "Scripts", "Shared", "server.cer");

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

    private async Task<bool> EnsureConnectedAsync()
    {
        // Already connected
        if (_client != null && _client.Connected && _stream != null)
        {
            return true;
        }

        // Clean up any stale connection state
        if (_client != null && !_client.Connected)
        {
            CleanupFailedConnection();
        }

        // Connection already in progress - wait for it
        if (_connectionTask != null && !_connectionTask.IsCompleted)
        {
            try
            {
                await _connectionTask;
                return _client != null && _client.Connected && _stream != null;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Connection attempt failed: {ex.Message}");
                _connectionTask = null;
                return false;
            }
        }

        // Start new connection
        _connectionTask = ConnectToServerAsync(serverHost, ServerConstants.DEFAULT_PORT);
        try
        {
            await _connectionTask;
            return _client != null && _client.Connected && _stream != null;
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"Connection attempt failed: {ex.Message}");
            _connectionTask = null;
            return false;
        }
    }

    private async Task ConnectToServerAsync(string host, int port)
    {
        try
        {
            if (_pinnedCertificate == null)
            {
                Debug.LogError("Cannot connect: Pinned certificate not loaded");
                throw new InvalidOperationException("Pinned certificate not loaded");
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
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to connect to server: {ex.Message}");
            CleanupFailedConnection();

            // Re-throw so EnsureConnectedAsync knows the connection failed
            throw;
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
        }

        return matches;
    }

    private async Task ListenForMessagesAsync()
    {
        try
        {
            while (_client != null && _client.Connected)
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

        bool handled = _handlerRegistry.ProcessMessage(message, this);
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

    public void UpdatePlayerLocation(string username, Vector3 position, Quaternion rotation)
    {
        _playerPositions[username] = position;
        _playerRotations[username] = rotation;
        OnLocationUpdate?.Invoke(username, position, rotation);
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

    public void InvokePlayerAuthenticate()
    {
        OnPlayerAuthenticate?.Invoke();
    }

    public async Task SendMessageAsync(Message message)
    {
        try
        {
            if (_stream == null || !_client.Connected)
            {
                InvokeError("NOT_CONNECTED", "Cannot send message: not connected to server");
                return;
            }

            string json = JsonConvert.SerializeObject(message);
            await MessageFraming.WriteMessageAsync(_stream, json);
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
            _playerPositions.Clear();
            _playerRotations.Clear();
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
        Cleanup();
    }

}
