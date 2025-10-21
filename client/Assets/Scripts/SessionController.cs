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
    [SerializeField]
    private TMP_Text _timeSyncText;
    [SerializeField]
    private TMP_Dropdown _serverListDropdown;
    [SerializeField]
    private Button _serverListRefreshButton;

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
        _serverListDropdown.onValueChanged.AddListener(OnServerListDropdownChanged);
        _serverListRefreshButton.onClick.AddListener(OnServerListRefreshButtonPressed);

        // Subscribe to server events
        _serverController.OnError += OnServerError;
        _serverController.OnSessionDisconnect += OnSessionDisconnect;
        _serverController.OnTimeSyncUpdate += OnTimeSyncUpdate;
        _serverController.OnServerListUpdate += OnServerListUpdate;

        _statusController.ClearStatus();
    }

    private void OnServerListUpdate()
    {
        var serverList = _serverController.GetServerList();
        _serverListDropdown.ClearOptions();
        var dropdownOptions = new System.Collections.Generic.List<TMP_Dropdown.OptionData>();
        foreach (var server in serverList)
        {
            string displayName = $"{server.name} ({server.host}:{server.port})";
            dropdownOptions.Add(new TMP_Dropdown.OptionData(displayName));
        }
        _serverListDropdown.AddOptions(dropdownOptions);
    }

    public void Update()
    {
        TimeSyncUpdateVisual();
    }

    private void OnServerError(string code, string message)
    {
        _statusController.SetStatus($"Server Error ({code}): {message}");
    }

    private void OnSessionDisconnect()
    {
        _statusController.SetStatus("Disconnected from server.");
    }

    private void OnTimeSyncUpdate()
    {
        TimeSyncUpdateVisual();
    }

    private void TimeSyncUpdateVisual()
    {
        if (_serverController.IsConnected())
        {
            long serverTimeMs = _serverController.GetServerTime();
            DateTime serverDateTime = NullZustand.TimeUtils.FromUnixTimestampMs(serverTimeMs);

            string militaryTime = serverDateTime.ToString("HH:mm:ss.ffff");
            _timeSyncText.text = $"Server Time: {militaryTime}";
        }
        else
        {
            _timeSyncText.text = "Not Connected";
        }
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

    private void OnServerListDropdownChanged(int index)
    {
        var serverList = _serverController.GetServerList();
        if (index < 0 || index >= serverList.Count)
        {
            Debug.LogError($"Invalid server index: {index}");
            return;
        }
        var selectedServer = serverList[index];
        _serverController.SetCurrentServer(selectedServer);
    }

    private void OnServerListRefreshButtonPressed()
    {
        _serverController.FetchServerList();
    }

    void OnDestroy()
    {
        if (_serverController != null)
        {
            _serverController.OnError -= OnServerError;
            _serverController.OnSessionDisconnect -= OnSessionDisconnect;
            _serverController.OnTimeSyncUpdate -= OnTimeSyncUpdate;
            _serverController.OnServerListUpdate -= OnServerListUpdate;
        }
        if (_loginButton != null)
        {
            _loginButton.onClick.RemoveListener(OnLoginButtonPressed);
        }
        if (_registerButton != null)
        {
            _registerButton.onClick.RemoveListener(OnRegisterButtonPressed);
        }
        if (_pingButton != null)
        {
            _pingButton.onClick.RemoveListener(OnPingButtonPressed);
        }
        if (_disconnectButton != null)
        {
            _disconnectButton.onClick.RemoveListener(OnDisconnectButtonPressed);
        }
        if (_serverListDropdown != null)
        {
            _serverListDropdown.onValueChanged.RemoveListener(OnServerListDropdownChanged);
        }
        if (_serverListRefreshButton != null)
        {
            _serverListRefreshButton.onClick.RemoveListener(OnServerListRefreshButtonPressed);
        }
    }

}
