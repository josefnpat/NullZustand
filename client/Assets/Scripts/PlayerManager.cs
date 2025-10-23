using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerManager : MonoBehaviour
{
    [SerializeField]
    private GameObject _playerPrefab;

    [SerializeField]
    private TMP_Text _statusText;
    [SerializeField]
    private TMP_Text _velocityText;

    [SerializeField]
    private InputActionReference _pitchActionReference;
    [SerializeField]
    private InputActionReference _yawActionReference;
    [SerializeField]
    private InputActionReference _rollActionReference;
    [SerializeField]
    private InputActionReference _throttleActionReference;

    private Dictionary<string, PlayerController> _playerControllers = new Dictionary<string, PlayerController>();
    private ServerController _serverController;
    private StatusController _statusController;

    // Track current rotation for incremental movement
    private Quaternion _currentRotation = Quaternion.identity;

    // Rate limiting for movement updates
    private float _lastMovementUpdateTime = 0f;
    private const float MOVEMENT_UPDATE_INTERVAL = 0.1f;
    private Quaternion _lastSentRotation = Quaternion.identity;

    // Throttle and velocity system
    private float _currentVelocity = 0f;
    private const float MAX_VELOCITY = 10f;
    private const float MIN_VELOCITY = 0f;
    private const float THROTTLE_ACCELERATION = 5f; // velocity units per second per throttle unit


    void Start()
    {
        _serverController = ServiceLocator.Get<ServerController>();
        _statusController = ServiceLocator.Get<StatusController>();
        _serverController.OnPlayerUpdate += OnPlayerUpdate;
        _serverController.OnSessionDisconnect += OnSessionDisconnect;

        _statusController.ClearStatus();
    }

    void Update()
    {
        if (_serverController.IsConnected() && _serverController.IsAuthenticated())
        {
            float pitchInput = _pitchActionReference.action.ReadValue<float>();
            float yawInput = _yawActionReference.action.ReadValue<float>();
            float rollInput = _rollActionReference.action.ReadValue<float>();
            float throttleInput = _throttleActionReference.action.ReadValue<float>();

            bool hasInput = Mathf.Abs(pitchInput) > 0.1f ||
                Mathf.Abs(yawInput) > 0.1f ||
                Mathf.Abs(rollInput) > 0.1f ||
                Mathf.Abs(throttleInput) > 0.1f;

            bool throttleChanged = false;

            if (hasInput)
            {
                float velocityChange = throttleInput * THROTTLE_ACCELERATION * Time.deltaTime;
                if (velocityChange != 0)
                {
                    throttleChanged = true;
                    _currentVelocity += velocityChange;
                    _currentVelocity = Mathf.Clamp(_currentVelocity, MIN_VELOCITY, MAX_VELOCITY);
                }

                float pitchChange = pitchInput * Time.deltaTime * 90f; // degrees per second
                float yawChange = yawInput * Time.deltaTime * 90f; // degrees per second
                float rollChange = rollInput * Time.deltaTime * 90f; // degrees per second

                Quaternion pitchRotation = Quaternion.AngleAxis(pitchChange, Vector3.right);
                Quaternion yawRotation = Quaternion.AngleAxis(yawChange, Vector3.up);
                Quaternion rollRotation = Quaternion.AngleAxis(rollChange, Vector3.forward);

                _currentRotation = _currentRotation * yawRotation * pitchRotation * rollRotation;
            }

            bool rotationChanged = Quaternion.Angle(_currentRotation, _lastSentRotation) > 1f;

            bool timeElapsed = Time.time - _lastMovementUpdateTime >= MOVEMENT_UPDATE_INTERVAL;

            if ((rotationChanged || throttleChanged) && timeElapsed)
            {
                _lastMovementUpdateTime = Time.time;
                _lastSentRotation = _currentRotation;
                _serverController.UpdatePosition(_currentRotation, _currentVelocity, OnUpdatePositionSuccess, OnUpdatePositionFailure);
            }

            UpdateVelocityVisual();
        }
    }

    private void UpdateVelocityVisual()
    {
        int velocityPercent = Mathf.FloorToInt(_currentVelocity / MAX_VELOCITY * 100);
        _velocityText.text = $"{_currentVelocity} u/s ({velocityPercent}%)";
    }

    private void OnPlayerUpdate(Player player)
    {
        PlayerController playerController;
        if (_playerControllers.ContainsKey(player.Username))
        {
            playerController = _playerControllers[player.Username];
        }
        else
        {
            GameObject go = Instantiate(_playerPrefab);
            go.name = $"Player({player.Username})";
            playerController = go.GetComponent<PlayerController>();
            _playerControllers.Add(player.Username, playerController);
        }
        playerController.SetPlayer(player);

        // Sync the current rotation and velocity if this is the current player
        Player currentPlayer = _serverController.GetCurrentPlayer();
        bool isCurrentPlayer = currentPlayer != null && player.Username == currentPlayer.Username;

        if (isCurrentPlayer)
        {
            _currentRotation = player.CurrentState.Rotation;
            _lastSentRotation = _currentRotation; // Also update the last sent rotation to avoid duplicate sends
            _currentVelocity = player.CurrentState.Velocity; // Sync velocity with server state
        }
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
            _serverController.OnPlayerUpdate -= OnPlayerUpdate;
            _serverController.OnSessionDisconnect -= OnSessionDisconnect;
        }
        ClearAllPlayers();
    }

}
