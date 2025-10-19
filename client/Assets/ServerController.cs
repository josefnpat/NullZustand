using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Newtonsoft.Json;

public class ServerController : MonoBehaviour
{
    private TcpClient _client;
    private NetworkStream _stream;

    async void Start()
    {
        await ConnectToServer("127.0.0.1", 7777);
    }

    private async Task ConnectToServer(string host, int port)
    {
        _client = new TcpClient();
        await _client.ConnectAsync(host, port);
        _stream = _client.GetStream();
        Debug.Log("Connected to server!");

        // Start listening for incoming messages
        _ = ListenForMessages();

        // Send a test Ping
        await SendMessageAsync(new Message
        {
            Type = "Ping",
            Payload = new { }
        });

        // Send a test LoginRequest
        await SendMessageAsync(new Message
        {
            Type = "LoginRequest",
            Payload = new { username = "PlayerOne", password = "secret" }
        });
    }

    private async Task ListenForMessages()
    {
        var buffer = new byte[4096];

        while (_client.Connected)
        {
            int bytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length);
            if (bytesRead == 0) break;

            var json = Encoding.UTF8.GetString(buffer, 0, bytesRead);
            var message = JsonConvert.DeserializeObject<Message>(json);

            HandleMessage(message);
        }

        Debug.Log("Disconnected from server.");
    }

    private void HandleMessage(Message message)
    {
        Debug.Log($"Received: {message.Type} | Payload: {JsonConvert.SerializeObject(message.Payload)}");
    }

    private async Task SendMessageAsync(Message message)
    {
        var json = JsonConvert.SerializeObject(message);
        var bytes = Encoding.UTF8.GetBytes(json);
        await _stream.WriteAsync(bytes, 0, bytes.Length);
    }

    [Serializable]
    public class Message
    {
        public string Type { get; set; }
        public object Payload { get; set; }
    }
}
