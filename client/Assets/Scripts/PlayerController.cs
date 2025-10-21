using UnityEngine;

[RequireComponent(typeof(TransformTweener))]
public class PlayerController : MonoBehaviour
{
    private TransformTweener _transformTweener;
    private PlayerState _lastServerState;
    private bool _hasReceivedUpdate = false;

    private void Awake()
    {
        _transformTweener = GetComponent<TransformTweener>();
        _lastServerState = new PlayerState();
    }

    private void Update()
    {
        if (_hasReceivedUpdate && _lastServerState.Velocity > 0.001f)
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

        // If velocity is very low, just tween to the exact position
        if (state.Velocity < 0.001f)
        {
            _transformTweener.TweenToLocation(state.Position, state.Rotation);
        }
    }
}
