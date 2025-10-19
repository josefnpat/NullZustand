using System;
using System.IO;
using System.Net.Sockets;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NullZustand;
using System.Collections.Generic;

public class ServerController : MonoBehaviour
{
    [SerializeField]
    private string serverHost = "127.0.0.1";

    private TcpClient _client;
    private SslStream _stream;
    private X509Certificate2 _pinnedCertificate;
    private long _lastLocationUpdateId = 0;
    private Dictionary<string, Vector3> _playerLocations = new Dictionary<string, Vector3>();

    public event Action<string, Vector3> OnLocationUpdate;

    void Awake()
    {
        ServiceLocator.Register<ServerController>(this);
    }

    void Start()
    {
        LoadPinnedCertificate();
        _ = ConnectToServerAsync(serverHost, ServerConstants.DEFAULT_PORT);
    }

    public async void Register(string username, string password)
    {
        await SendMessageAsync(new Message
        {
            Type = MessageTypes.REGISTER_REQUEST,
            Payload = new { username = username, password = password }
        });
    }

    public async void Login(string username, string password)
    {
        await SendMessageAsync(new Message
        {
            Type = MessageTypes.LOGIN_REQUEST,
            Payload = new { username = username, password = password }
        });
    }

    public async void SendPing()
    {
        await SendMessageAsync(new Message
        {
            Type = MessageTypes.PING,
            Payload = new { }
        });
    }

    public async void UpdatePosition(float x, float y, float z)
    {
        await SendMessageAsync(new Message
        {
            Type = MessageTypes.UPDATE_POSITION_REQUEST,
            Payload = new { x = x, y = y, z = z }
        });
    }

    public async void GetLocationUpdates()
    {
        await SendMessageAsync(new Message
        {
            Type = MessageTypes.LOCATION_UPDATES_REQUEST,
            Payload = new { lastUpdateId = _lastLocationUpdateId }
        });
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

    private async Task ConnectToServerAsync(string host, int port)
    {
        try
        {
            if (_pinnedCertificate == null)
            {
                Debug.LogError("Cannot connect: Pinned certificate not loaded");
                return;
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
                    Debug.LogError("Connection closed or read failed");
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

        try
        {
            JObject payload = JObject.FromObject(message.Payload);

            // Handle LOGIN_RESPONSE - initial sync of all players
            if (message.Type == MessageTypes.LOGIN_RESPONSE)
            {
                if (payload["lastLocationUpdateId"] != null)
                {
                    _lastLocationUpdateId = payload["lastLocationUpdateId"].Value<long>();
                }

                if (payload["allPlayers"] != null)
                {
                    var allPlayers = payload["allPlayers"] as JArray;
                    foreach (var player in allPlayers)
                    {
                        string username = player["username"].Value<string>();
                        float x = player["x"].Value<float>();
                        float y = player["y"].Value<float>();
                        float z = player["z"].Value<float>();

                        var position = new Vector3(x, y, z);
                        _playerLocations[username] = position;
                        OnLocationUpdate?.Invoke(username, position);
                    }
                }
            }

            // Handle LOCATION_UPDATES_RESPONSE - incremental updates
            if (message.Type == MessageTypes.LOCATION_UPDATES_RESPONSE)
            {
                if (payload["lastLocationUpdateId"] != null)
                {
                    _lastLocationUpdateId = payload["lastLocationUpdateId"].Value<long>();
                }

                if (payload["updates"] != null)
                {
                    var updates = payload["updates"] as JArray;
                    foreach (var update in updates)
                    {
                        string username = update["username"].Value<string>();
                        float x = update["x"].Value<float>();
                        float y = update["y"].Value<float>();
                        float z = update["z"].Value<float>();

                        var position = new Vector3(x, y, z);
                        _playerLocations[username] = position;
                        OnLocationUpdate?.Invoke(username, position);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"Failed to process message: {ex.Message}");
        }
    }

    private async Task SendMessageAsync(Message message)
    {
        try
        {
            if (_stream == null || !_client.Connected)
            {
                Debug.LogWarning("Cannot send message: not connected to server");
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
            _lastLocationUpdateId = 0;
            _playerLocations.Clear();
        }
    }

    void OnDestroy()
    {
        Cleanup();
    }

}
