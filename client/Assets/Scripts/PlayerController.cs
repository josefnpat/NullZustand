using UnityEngine;

[RequireComponent(typeof(TransformTweener))]
public class PlayerController : MonoBehaviour
{
    private TransformTweener _transformTweener;
    private PlayerState _lastServerState;
    private bool _hasReceivedUpdate = false;
    private bool _isFirstUpdate = true;

    private void Awake()
    {
        _transformTweener = GetComponent<TransformTweener>();
        _lastServerState = new PlayerState();
    }

    private void Update()
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

    public void SetLocation(PlayerState state)
    {
        _lastServerState.Position = state.Position;
        _lastServerState.Rotation = state.Rotation;
        _lastServerState.Velocity = state.Velocity;
        _lastServerState.TimestampMs = state.TimestampMs;
        _hasReceivedUpdate = true;

        long currentTimeMs = NullZustand.TimeUtils.GetUnixTimestampMs();
        float elapsedSeconds = (currentTimeMs - state.TimestampMs) / 1000.0f;

        Vector3 currentPosition = state.Position;
        if (state.Velocity != 0f && elapsedSeconds > 0)
        {
            Vector3 forward = state.Rotation * Vector3.forward;
            currentPosition = state.Position + forward * state.Velocity * elapsedSeconds;
        }

        if (_isFirstUpdate)
        {
            _transformTweener.SetLocationImmediate(currentPosition, state.Rotation);
            _isFirstUpdate = false;
        }
        else if (state.Velocity == 0f)
        {
            _transformTweener.TweenToLocation(currentPosition, state.Rotation);
        }
    }
}
