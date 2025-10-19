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
    private TMP_Text _statusText;
    [SerializeField]
    private Button _pingButton;
    [SerializeField]
    private TMP_InputField _xInputField;
    [SerializeField]
    private TMP_InputField _yInputField;
    [SerializeField]
    private TMP_InputField _zInputField;
    [SerializeField]
    private Button _updatePositionButton;
    [SerializeField]
    private Button _getLocationUpdatesButton;

    private ServerController _serverController;

    public void Start()
    {
        _serverController = ServiceLocator.Get<ServerController>();

        _loginButton.onClick.AddListener(OnLoginButtonPressed);
        _registerButton.onClick.AddListener(OnRegisterButtonPressed);
        _pingButton.onClick.AddListener(OnPingButtonPressed);
        _updatePositionButton.onClick.AddListener(OnUpdatePositionButtonPressed);
        _getLocationUpdatesButton.onClick.AddListener(OnGetLocationUpdatesButtonPressed);

        // Subscribe to error events
        _serverController.OnError += OnServerError;

        ClearStatus();
    }

    private void OnServerError(string code, string message)
    {
        SetStatus($"Server Error ({code}): {message}");
    }

    public void OnLoginButtonPressed()
    {
        string username = _usernameInputField.text;
        string password = _passwordInputField.text;
        ClearStatus();
        
        // Client-side validation
        if (string.IsNullOrWhiteSpace(username))
        {
            SetStatus("Username cannot be empty");
            return;
        }
        
        if (string.IsNullOrWhiteSpace(password))
        {
            SetStatus("Password cannot be empty");
            return;
        }
        
        if (username.Length > ValidationConstants.MAX_USERNAME_LENGTH)
        {
            SetStatus($"Username must be at most {ValidationConstants.MAX_USERNAME_LENGTH} characters");
            return;
        }
        
        if (password.Length > ValidationConstants.MAX_PASSWORD_LENGTH)
        {
            SetStatus($"Password must be at most {ValidationConstants.MAX_PASSWORD_LENGTH} characters");
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

            SetStatus($"Login successful! {playerCount} player(s) online.");
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"Failed to parse login success payload: {ex.Message}");
        }
    }

    private void OnLoginFailure(string error)
    {
        SetStatus($"Login failed: {error}");
    }

    public void OnRegisterButtonPressed()
    {
        string username = _usernameInputField.text;
        string password = _passwordInputField.text;
        ClearStatus();
        
        // Client-side validation
        if (string.IsNullOrWhiteSpace(username))
        {
            SetStatus("Username cannot be empty");
            return;
        }
        
        if (username.Length < ValidationConstants.MIN_USERNAME_LENGTH)
        {
            SetStatus($"Username must be at least {ValidationConstants.MIN_USERNAME_LENGTH} characters");
            return;
        }
        
        if (username.Length > ValidationConstants.MAX_USERNAME_LENGTH)
        {
            SetStatus($"Username must be at most {ValidationConstants.MAX_USERNAME_LENGTH} characters");
            return;
        }
        
        if (string.IsNullOrWhiteSpace(password))
        {
            SetStatus("Password cannot be empty");
            return;
        }
        
        if (password.Length > ValidationConstants.MAX_PASSWORD_LENGTH)
        {
            SetStatus($"Password must be at most {ValidationConstants.MAX_PASSWORD_LENGTH} characters");
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
            SetStatus($"Registration successful! You can now log in as '{username}'.");
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"Failed to parse register success payload: {ex.Message}");
        }
    }

    private void OnRegisterFail(string error)
    {
        SetStatus($"Registration failed: {error}");
    }

    public void OnPingButtonPressed()
    {
        ClearStatus();
        _serverController.SendPing(OnPingSuccess, OnPingFailure);
    }

    private void OnPingSuccess(object payload)
    {
        try
        {
            JObject data = JObject.FromObject(payload);
            string serverTime = data["time"]?.Value<string>();
            SetStatus($"Pong! Server time: {serverTime}");
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"Failed to parse ping success payload: {ex.Message}");
        }
    }

    private void OnPingFailure(string error)
    {
        SetStatus($"Ping failed: {error}");
    }

    public void OnUpdatePositionButtonPressed()
    {
        ClearStatus();

        // Validate and parse all input fields
        bool xValid = float.TryParse(_xInputField.text, out float x);
        bool yValid = float.TryParse(_yInputField.text, out float y);
        bool zValid = float.TryParse(_zInputField.text, out float z);

        // Check for validation errors
        if (!xValid || !yValid || !zValid)
        {
            SetStatus("Invalid coordinates. Please enter valid numbers.");
            return;
        }
        
        // Validate coordinate ranges
        if (!IsValidCoordinate(x) || !IsValidCoordinate(y) || !IsValidCoordinate(z))
        {
            SetStatus($"Coordinates must be between {ValidationConstants.MIN_COORDINATE} and {ValidationConstants.MAX_COORDINATE}");
            return;
        }
        
        _serverController.UpdatePosition(x, y, z, OnUpdatePositionSuccess, OnUpdatePositionFailure);
    }
    
    private bool IsValidCoordinate(float value)
    {
        return !float.IsNaN(value) && 
               !float.IsInfinity(value) && 
               value >= ValidationConstants.MIN_COORDINATE && 
               value <= ValidationConstants.MAX_COORDINATE;
    }

    private void OnUpdatePositionSuccess(object payload)
    {
        try
        {
            JObject data = JObject.FromObject(payload);
            long updateId = data["updateId"]?.Value<long>() ?? 0;

            SetStatus($"Position updated successfully! Update ID: {updateId}");
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"Failed to parse update position success payload: {ex.Message}");
        }
    }

    private void OnUpdatePositionFailure(string error)
    {
        SetStatus($"Position update failed: {error}");
    }

    private void OnGetLocationUpdatesButtonPressed()
    {
        ClearStatus();
        _serverController.GetLocationUpdates(OnGetLocationUpdatesSuccess, OnGetLocationUpdatesFailure);
    }

    private void OnGetLocationUpdatesSuccess(object payload)
    {
        try
        {
            JObject data = JObject.FromObject(payload);
            long lastUpdateId = data["lastLocationUpdateId"]?.Value<long>() ?? 0;

            int updateCount = 0;
            if (data["updates"] is JArray updates)
            {
                updateCount = updates.Count;
            }

            if (updateCount > 0)
            {
                SetStatus($"Received {updateCount} location update(s). Update ID: {lastUpdateId}");
            }
            else
            {
                SetStatus("No new location updates.");
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"Failed to parse location updates success payload: {ex.Message}");
        }
    }

    private void OnGetLocationUpdatesFailure(string error)
    {
        SetStatus($"Location updates failed: {error}");
    }

    private void ClearStatus()
    {
        SetStatus(string.Empty);
    }

    private void SetStatus(string status)
    {
        _statusText.text = status;
    }

    void OnDestroy()
    {
        if (_serverController != null)
        {
            _serverController.OnError -= OnServerError;
        }
    }

}
