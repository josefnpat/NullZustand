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
    private TMP_InputField _xInputField;
    [SerializeField]
    private TMP_InputField _yInputField;
    [SerializeField]
    private TMP_InputField _zInputField;
    [SerializeField]
    private Button _updatePositionButton;
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
        _updatePositionButton.onClick.AddListener(OnUpdatePositionButtonPressed);
        _getLocationUpdatesButton.onClick.AddListener(OnGetLocationUpdatesButtonPressed);

        _statusController.ClearStatus();
    }

    private void OnLocationUpdate(string username, Vector3 position)
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
        playerController.SetPosition(position);
    }

    public void OnUpdatePositionButtonPressed()
    {
        _statusController.ClearStatus();

        // Validate and parse all input fields
        bool xValid = float.TryParse(_xInputField.text, out float x);
        bool yValid = float.TryParse(_yInputField.text, out float y);
        bool zValid = float.TryParse(_zInputField.text, out float z);

        if (!xValid || !yValid || !zValid)
        {
            _statusController.SetStatus("Invalid coordinates. Please enter valid numbers.");
            return;
        }

        if (!IsValidCoordinate(x) || !IsValidCoordinate(y) || !IsValidCoordinate(z))
        {
            _statusController.SetStatus($"Coordinates must be between {ValidationConstants.MIN_COORDINATE} and {ValidationConstants.MAX_COORDINATE}");
            return;
        }

        _serverController.UpdatePosition(x, y, z, OnUpdatePositionSuccess, OnUpdatePositionFailure);
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

    private bool IsValidCoordinate(float value)
    {
        return !float.IsNaN(value) &&
            !float.IsInfinity(value) &&
            value >= ValidationConstants.MIN_COORDINATE &&
            value <= ValidationConstants.MAX_COORDINATE;
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
        }
        ClearAllPlayers();
    }

}
