using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(TransformTweener))]
public class PlayerController : MonoBehaviour
{

    private TransformTweener _transformTweener;

    [SerializeField]
    private Camera _playerCamera;
    public Camera PlayerCamera { get { return _playerCamera; } }
    [SerializeField]
    private TransformTweener _playerCameraTransformTweener;

    [SerializeField]
    private List<Transform> _cameraLocations;

    private int _currentCameraLocationIndex = 0;

    private PlayerState _lastServerState;
    private bool _hasReceivedUpdate = false;
    private bool _isFirstUpdate = true;

    void Awake()
    {
        _transformTweener = GetComponent<TransformTweener>();
        _lastServerState = new PlayerState();
    }

    void Start()
    {
        Transform firstLocation = _cameraLocations[0];
        _playerCameraTransformTweener.SetLocationImmediate(firstLocation.position, firstLocation.rotation);
    }

    void Update()
    {
        if (_hasReceivedUpdate && _lastServerState.Velocity != 0f)
        {
            // Calculate predicted position based on velocity and time elapsed
            long currentTimeMs = NullZustand.TimeUtils.GetUnixTimestampMs();
            float elapsedSeconds = (currentTimeMs - _lastServerState.TimestampMs) / 1000.0f;

            // Calculate movement direction from rotation (forward vector)
            Vector3 forward = _lastServerState.Rotation * Vector3.forward;

            // Calculate predicted position
            Vector3 predictedPosition = _lastServerState.Position + forward * _lastServerState.Velocity * elapsedSeconds;

            // Smoothly move to predicted position
            _transformTweener.TweenToLocation(predictedPosition, _lastServerState.Rotation);
        }
    }

    public void SetPlayer(Player player)
    {
        _lastServerState.Position = player.CurrentState.Position;
        _lastServerState.Rotation = player.CurrentState.Rotation;
        _lastServerState.Velocity = player.CurrentState.Velocity;
        _lastServerState.TimestampMs = player.CurrentState.TimestampMs;
        _hasReceivedUpdate = true;

        long currentTimeMs = NullZustand.TimeUtils.GetUnixTimestampMs();
        float elapsedSeconds = (currentTimeMs - player.CurrentState.TimestampMs) / 1000.0f;

        Vector3 currentPosition = player.CurrentState.Position;
        if (player.CurrentState.Velocity != 0f && elapsedSeconds > 0)
        {
            Vector3 forward = player.CurrentState.Rotation * Vector3.forward;
            currentPosition = player.CurrentState.Position + forward * player.CurrentState.Velocity * elapsedSeconds;
        }

        if (_isFirstUpdate)
        {
            _transformTweener.SetLocationImmediate(currentPosition, player.CurrentState.Rotation);
            _isFirstUpdate = false;
        }
        else if (player.CurrentState.Velocity == 0f)
        {
            _transformTweener.TweenToLocation(currentPosition, player.CurrentState.Rotation);
        }
    }

    public void CycleCameraLocation()
    {
        _currentCameraLocationIndex = (_currentCameraLocationIndex + 1) % _cameraLocations.Count;
        SetCameraLocation(_currentCameraLocationIndex);
    }

    public void SetCameraLocation(int index)
    {
        if (_cameraLocations == null || _cameraLocations.Count == 0)
        {
            Debug.LogWarning("[PlayerController] No camera locations available for cycling");
            return;
        }
        Transform targetLocation = _cameraLocations[index];
        _playerCameraTransformTweener.TweenToLocation(targetLocation.position, targetLocation.rotation);
    }

}
