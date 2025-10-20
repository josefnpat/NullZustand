using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Newtonsoft.Json.Linq;
using NullZustand;

public class SessionController : MonoBehaviour
{
    [SerializeField]
    private TMP_InputField _usernameInputField;
    [SerializeField]
    private TMP_InputField _passwordInputField;
    [SerializeField]
    private Button _loginButton;
    [SerializeField]
    private Button _registerButton;
    [SerializeField]
    private Button _disconnectButton;

    [SerializeField]
    private Button _pingButton;

    private ServerController _serverController;
    private StatusController _statusController;

    public void Start()
    {
        _serverController = ServiceLocator.Get<ServerController>();
        _statusController = ServiceLocator.Get<StatusController>();

        _loginButton.onClick.AddListener(OnLoginButtonPressed);
        _registerButton.onClick.AddListener(OnRegisterButtonPressed);
        _pingButton.onClick.AddListener(OnPingButtonPressed);
        _disconnectButton.onClick.AddListener(OnDisconnectButtonPressed);

        // Subscribe to error events
        _serverController.OnError += OnServerError;
        _serverController.OnSessionDisconnect += OnSessionDisconnect;

        _statusController.ClearStatus();
    }

    private void OnServerError(string code, string message)
    {
        _statusController.SetStatus($"Server Error ({code}): {message}");
    }

    private void OnSessionDisconnect()
    {
        _statusController.SetStatus("Disconnected from server.");
    }

    public void OnLoginButtonPressed()
    {
        string username = _usernameInputField.text;
        string password = _passwordInputField.text;
        _statusController.ClearStatus();
        
        // Client-side validation
        if (string.IsNullOrWhiteSpace(username))
        {
            _statusController.SetStatus("Username cannot be empty");
            return;
        }
        
        if (string.IsNullOrWhiteSpace(password))
        {
            _statusController.SetStatus("Password cannot be empty");
            return;
        }
        
        if (password.Length < ValidationConstants.MIN_PASSWORD_LENGTH)
        {
            _statusController.SetStatus($"Password must be at least {ValidationConstants.MIN_PASSWORD_LENGTH} characters");
            return;
        }

        if (username.Length > ValidationConstants.MAX_USERNAME_LENGTH)
        {
            _statusController.SetStatus($"Username must be at most {ValidationConstants.MAX_USERNAME_LENGTH} characters");
            return;
        }
        
        if (password.Length > ValidationConstants.MAX_PASSWORD_LENGTH)
        {
            _statusController.SetStatus($"Password must be at most {ValidationConstants.MAX_PASSWORD_LENGTH} characters");
            return;
        }
        
        _serverController.Login(username, password, OnLoginSuccess, OnLoginFailure);
    }

    private void OnLoginSuccess(object payload)
    {
        try
        {
            JObject data = JObject.FromObject(payload);

            // Count players
            int playerCount = 0;
            if (data["allPlayers"] is JArray allPlayers)
            {
                playerCount = allPlayers.Count;
            }

            _statusController.SetStatus($"Login successful! {playerCount} player(s) online.");
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"Failed to parse login success payload: {ex.Message}");
        }
    }

    private void OnLoginFailure(string error)
    {
        _statusController.SetStatus($"Login failed: {error}");
    }

    public void OnRegisterButtonPressed()
    {
        string username = _usernameInputField.text;
        string password = _passwordInputField.text;
        _statusController.ClearStatus();
        
        // Client-side validation
        if (string.IsNullOrWhiteSpace(username))
        {
            _statusController.SetStatus("Username cannot be empty");
            return;
        }
        
        if (username.Length < ValidationConstants.MIN_USERNAME_LENGTH)
        {
            _statusController.SetStatus($"Username must be at least {ValidationConstants.MIN_USERNAME_LENGTH} characters");
            return;
        }
        
        if (username.Length > ValidationConstants.MAX_USERNAME_LENGTH)
        {
            _statusController.SetStatus($"Username must be at most {ValidationConstants.MAX_USERNAME_LENGTH} characters");
            return;
        }
        
        if (string.IsNullOrWhiteSpace(password))
        {
            _statusController.SetStatus("Password cannot be empty");
            return;
        }
        
        if (password.Length < ValidationConstants.MIN_PASSWORD_LENGTH)
        {
            _statusController.SetStatus($"Password must be at least {ValidationConstants.MIN_PASSWORD_LENGTH} characters");
            return;
        }

        if (password.Length > ValidationConstants.MAX_PASSWORD_LENGTH)
        {
            _statusController.SetStatus($"Password must be at most {ValidationConstants.MAX_PASSWORD_LENGTH} characters");
            return;
        }
        
        _serverController.Register(username, password, OnRegisterSuccess, OnRegisterFail);
    }

    private void OnRegisterSuccess(object payload)
    {
        try
        {
            JObject data = JObject.FromObject(payload);
            string username = data["username"]?.Value<string>();
            _statusController.SetStatus($"Registration successful! You can now log in as '{username}'.");
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"Failed to parse register success payload: {ex.Message}");
        }
    }

    private void OnRegisterFail(string error)
    {
        _statusController.SetStatus($"Registration failed: {error}");
    }

    public void OnPingButtonPressed()
    {
        _statusController.ClearStatus();
        _serverController.SendPing(OnPingSuccess, OnPingFailure);
    }

    private void OnPingSuccess(object payload)
    {
        try
        {
            JObject data = JObject.FromObject(payload);
            string serverTime = data["time"]?.Value<string>();
            _statusController.SetStatus($"Pong! Server time: {serverTime}");
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"Failed to parse ping success payload: {ex.Message}");
        }
    }

    private void OnPingFailure(string error)
    {
        _statusController.SetStatus($"Ping failed: {error}");
    }

    public void OnDisconnectButtonPressed()
    {
        _serverController.Disconnect();
        _statusController.SetStatus("Disconnected from server.");
    }

    void OnDestroy()
    {
        if (_serverController != null)
        {
            _serverController.OnError -= OnServerError;
            _serverController.OnSessionDisconnect -= OnSessionDisconnect;
        }
    }

}
