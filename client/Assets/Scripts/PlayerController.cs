using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerController : MonoBehaviour
{
    private ProfileManager _profileManager;
    private CameraManager _cameraManager;

    [SerializeField]
    private Camera _playerCamera;
    public Camera PlayerCamera { get { return _playerCamera; } }
    [SerializeField]
    private TransformTweener _playerCameraTransformTweener;
    [SerializeField]
    private List<Transform> _cameraLocations;
    [SerializeField]
    private GameObject _playerInfoGameObject;
    [SerializeField]
    private Image _playerInfoImage;
    [SerializeField]
    private TMP_Text _playerInfoText;

    private int _currentCameraLocationIndex = 0;
    private Player _player;
    private bool _isCurrentPlayer;

    public Player Player { get { return _player; } }
    public long EntityId => _player?.EntityId ?? EntityManager.INVALID_ENTITY_ID;

    public void Awake()
    {
        _playerInfoGameObject.SetActive(false);
    }

    public void Setup()
    {
        _profileManager = ServiceLocator.Get<ProfileManager>();
    }

    void Start()
    {
        if (_cameraLocations != null && _cameraLocations.Count > 0)
        {
            Transform firstLocation = _cameraLocations[0];
            _playerCameraTransformTweener.SetLocationImmediate(firstLocation.position, firstLocation.rotation);
        }
        _cameraManager = ServiceLocator.Get<CameraManager>();
    }

    void Update()
    {
        if (_player != null && !_isCurrentPlayer)
        {
            Camera currentCamera = _cameraManager.GetCurrentCamera();
            _playerInfoGameObject.transform.LookAt(currentCamera.transform);
        }
    }

    public void SetPlayer(Player player, bool isCurrentPlayer)
    {
        _player = player;
        _isCurrentPlayer = isCurrentPlayer;
        _playerInfoText.text = $"{player.Username}";
        _playerInfoImage.sprite = _profileManager.FindProfileImage(player);
        _playerInfoGameObject.SetActive(!isCurrentPlayer);
    }

    public void UpdatePlayerProfile(Profile profile)
    {
        if (_player != null)
        {
            _player.Profile = profile;
            _playerInfoImage.sprite = _profileManager.FindProfileImage(_player);
        }
        else
        {
            Debug.LogWarning($"[PlayerController] Attempted to update profile for null player");
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
