using UnityEngine;

[RequireComponent(typeof(TransformTweener))]
public class EntityController : MonoBehaviour
{
    private TransformTweener _transformTweener;
    private Entity _lastServerState;
    private bool _hasReceivedUpdate = false;
    private bool _isFirstUpdate = true;

    void Start()
    {
        _transformTweener = GetComponent<TransformTweener>();
        _lastServerState = new Entity();
    }

    void Update()
    {
        if (_hasReceivedUpdate && _lastServerState.Velocity != 0f)
        {
            long currentTimeMs = NullZustand.TimeUtils.GetUnixTimestampMs();
            float elapsedSeconds = (currentTimeMs - _lastServerState.TimestampMs) / 1000.0f;
            Vector3 forward = _lastServerState.Rotation * Vector3.forward;
            Vector3 predictedPosition = _lastServerState.Position + forward * _lastServerState.Velocity * elapsedSeconds;
            _transformTweener.TweenToLocation(predictedPosition, _lastServerState.Rotation);
        }
    }

    public void UpdateMovement(Entity entity)
    {
        if (entity == null)
        {
            Debug.LogWarning("[EntityController] UpdateMovement called with null entity");
            return;
        }

        _hasReceivedUpdate = true;

        _lastServerState.Position = entity.Position;
        _lastServerState.Rotation = entity.Rotation;
        _lastServerState.Velocity = entity.Velocity;
        _lastServerState.TimestampMs = entity.TimestampMs;

        long currentTimeMs = NullZustand.TimeUtils.GetUnixTimestampMs();
        float elapsedSeconds = (currentTimeMs - entity.TimestampMs) / 1000.0f;

        Vector3 currentPosition = entity.Position;
        if (entity.Velocity != 0f && elapsedSeconds > 0)
        {
            Vector3 forward = entity.Rotation * Vector3.forward;
            currentPosition = entity.Position + forward * entity.Velocity * elapsedSeconds;
        }

        if (_isFirstUpdate)
        {
            _transformTweener.SetLocationImmediate(currentPosition, entity.Rotation);
            _isFirstUpdate = false;
        }
        else if (entity.Velocity == 0f)
        {
            _transformTweener.TweenToLocation(currentPosition, entity.Rotation);
        }
    }
}