using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Newtonsoft.Json;
using NullZustand;

public class ServerController : MonoBehaviour
{
    [SerializeField]
    private string serverHost = "127.0.0.1";
    
    private TcpClient _client;
    private NetworkStream _stream;

    void Start()
    {
        _ = ConnectToServerAsync(serverHost, ServerConstants.DEFAULT_PORT);
    }

    private async Task ConnectToServerAsync(string host, int port)
    {
        try
        {
            _client = new TcpClient();
            await _client.ConnectAsync(host, port);
            _stream = _client.GetStream();
            Debug.Log("Connected to server!");

            // Start listening for incoming messages
            _ = ListenForMessagesAsync();

            // Send a test Ping
            await SendMessageAsync(new Message
            {
                Type = MessageTypes.PING,
                Payload = new { }
            });

            // Send a test LoginRequest
            await SendMessageAsync(new Message
            {
                Type = MessageTypes.LOGIN_REQUEST,
                Payload = new { username = "PlayerOne", password = "secret" }
            });
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to connect to server: {ex.Message}");
            // TODO: Implement retry logic
        }
    }

    private async Task ListenForMessagesAsync()
    {
        byte[] buffer = new byte[4096];

        try
        {
            while (_client != null && _client.Connected)
            {
                int bytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length);
                if (bytesRead == 0) break;

                string json = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                Message message = JsonConvert.DeserializeObject<Message>(json);

                HandleMessage(message);
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
            byte[] bytes = Encoding.UTF8.GetBytes(json);
            await _stream.WriteAsync(bytes, 0, bytes.Length);
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
        }
    }

    void OnDestroy()
    {
        Cleanup();
    }

}
