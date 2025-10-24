using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField]
    private Camera _playerCamera;
    public Camera PlayerCamera { get { return _playerCamera; } }
    [SerializeField]
    private TransformTweener _playerCameraTransformTweener;

    [SerializeField]
    private List<Transform> _cameraLocations;

    private int _currentCameraLocationIndex = 0;
    private Player _player;
    public long EntityId => _player?.EntityId ?? EntityManager.INVALID_ENTITY_ID;

    public void Start()
    {
        if (_cameraLocations != null && _cameraLocations.Count > 0)
        {
            Transform firstLocation = _cameraLocations[0];
            _playerCameraTransformTweener.SetLocationImmediate(firstLocation.position, firstLocation.rotation);
        }
    }

    public void SetPlayer(Player player)
    {
        _player = player;
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
