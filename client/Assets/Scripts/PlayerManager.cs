using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using NullZustand;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerManager : MonoBehaviour
{
    [SerializeField]
    private GameObject _playerPrefab;

    [SerializeField]
    private TMP_Text _statusText;
    [SerializeField]
    private TMP_InputField _xRotationInputField;
    [SerializeField]
    private TMP_InputField _yRotationInputField;
    [SerializeField]
    private TMP_InputField _zRotationInputField;
    [SerializeField]
    private TMP_InputField _wRotationInputField;
    [SerializeField]
    private TMP_InputField _velocityInputField;
    [SerializeField]
    private Button _updateLocationButton;
    [SerializeField]
    private Button _getLocationUpdatesButton;

    private Dictionary<string, PlayerController> _playerControllers = new Dictionary<string, PlayerController>();
    private ServerController _serverController;
    private StatusController _statusController;

    public void Start()
    {
        _serverController = ServiceLocator.Get<ServerController>();
        _statusController = ServiceLocator.Get<StatusController>();
        _serverController.OnLocationUpdate += OnLocationUpdate;
        _serverController.OnSessionDisconnect += OnSessionDisconnect;
        _updateLocationButton.onClick.AddListener(OnUpdateLocationButtonPressed);
        _getLocationUpdatesButton.onClick.AddListener(OnGetLocationUpdatesButtonPressed);

        _statusController.ClearStatus();
    }

    private void OnLocationUpdate(string username, PlayerState state)
    {
        PlayerController playerController;
        if (_playerControllers.ContainsKey(username))
        {
            playerController = _playerControllers[username];
        }
        else
        {
            GameObject go = Instantiate(_playerPrefab);
            playerController = go.GetComponent<PlayerController>();
            _playerControllers.Add(username, playerController);
        }
        playerController.SetLocation(state);
    }

    public void OnUpdateLocationButtonPressed()
    {
        _statusController.ClearStatus();

        // Validate and parse rotation and velocity input fields
        bool xValidRotation = float.TryParse(_xRotationInputField.text, out float xRot);
        bool yValidRotation = float.TryParse(_yRotationInputField.text, out float yRot);
        bool zValidRotation = float.TryParse(_zRotationInputField.text, out float zRot);
        bool wValidRotation = float.TryParse(_wRotationInputField.text, out float wRot);
        bool velocityValid = float.TryParse(_velocityInputField.text, out float velocity);

        if (!xValidRotation || !yValidRotation || !zValidRotation || !wValidRotation)
        {
            _statusController.SetStatus("Invalid rotation. Please enter valid numbers for quaternion components.");
            return;
        }

        if (!IsValidRotation(xRot) || !IsValidRotation(yRot) || !IsValidRotation(zRot) || !IsValidRotation(wRot))
        {
            _statusController.SetStatus("Invalid rotation. Quaternion components must be valid numbers.");
            return;
        }

        if (!velocityValid)
        {
            _statusController.SetStatus("Invalid velocity. Please enter a valid number.");
            return;
        }

        if (!IsValidVelocity(velocity))
        {
            _statusController.SetStatus("Invalid velocity. Velocity must be a valid number.");
            return;
        }

        // Validate velocity is non-negative
        if (velocity < 0)
        {
            _statusController.SetStatus("Velocity must be non-negative.");
            return;
        }

        var rotation = new Quaternion(xRot, yRot, zRot, wRot);

        _serverController.UpdatePosition(rotation, velocity, OnUpdatePositionSuccess, OnUpdatePositionFailure);
    }

    private void OnUpdatePositionSuccess(object payload)
    {
        try
        {
            JObject data = JObject.FromObject(payload);
            long updateId = data["updateId"]?.Value<long>() ?? 0;

            _statusController.SetStatus($"Position updated successfully! Update ID: {updateId}");
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"Failed to parse update position success payload: {ex.Message}");
        }
    }

    private void OnUpdatePositionFailure(string error)
    {
        _statusController.SetStatus($"Position update failed: {error}");
    }

    private bool IsValidRotation(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value);
    }

    private bool IsValidVelocity(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value);
    }

    private void OnGetLocationUpdatesButtonPressed()
    {
        _statusController.ClearStatus();
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
                _statusController.SetStatus($"Received {updateCount} location update(s). Update ID: {lastUpdateId}");
            }
            else
            {
                _statusController.SetStatus("No new location updates.");
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"Failed to parse location updates success payload: {ex.Message}");
        }
    }

    private void OnGetLocationUpdatesFailure(string error)
    {
        _statusController.SetStatus($"Location updates failed: {error}");
    }

    private void OnSessionDisconnect()
    {
        ClearAllPlayers();
    }

    public void ClearAllPlayers()
    {
        foreach (var controller in _playerControllers.Values)
        {
            if (controller != null && controller.gameObject != null)
            {
                Destroy(controller.gameObject);
            }
        }
        _playerControllers.Clear();
    }

    void OnDestroy()
    {
        if (_serverController != null)
        {
            _serverController.OnLocationUpdate -= OnLocationUpdate;
            _serverController.OnSessionDisconnect -= OnSessionDisconnect;
        }
        ClearAllPlayers();
    }

}
