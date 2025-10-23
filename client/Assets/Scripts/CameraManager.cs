using UnityEngine;
using System.Collections.Generic;

public class CameraManager : MonoBehaviour
{
    private Camera _mainCamera;
    
    private Camera _currentPlayerCamera;
    private Dictionary<string, PlayerController> _playerControllers = new Dictionary<string, PlayerController>();
    private ServerController _serverController;

    void Awake()
    {
        ServiceLocator.Register<CameraManager>(this);
        _mainCamera = Camera.main;
    }

    void Start()
    {
        _serverController = ServiceLocator.Get<ServerController>();
        _serverController.OnPlayerAuthenticate += OnPlayerAuthenticated;
        _serverController.OnSessionDisconnect += OnSessionDisconnect;
        SetMainCameraActive();
    }

    void OnDestroy()
    {
        _serverController.OnPlayerAuthenticate -= OnPlayerAuthenticated;
        _serverController.OnSessionDisconnect -= OnSessionDisconnect;
    }

    public void RegisterPlayerController(string username, PlayerController playerController)
    {
        if (playerController == null)
        {
            Debug.LogError($"[CameraManager] Attempted to register null PlayerController for {username}");
            return;
        }

        if (_playerControllers.ContainsKey(username))
        {
            Debug.LogError($"[CameraManager] Attempted to register already registered user: {username}");
            return;
        }

        _playerControllers[username] = playerController;

        if (_serverController.IsAuthenticated())
        {
            Player currentPlayer = _serverController.GetCurrentPlayer();
            if (currentPlayer != null && currentPlayer.Username == username)
            {
                SwitchToPlayerCamera(playerController);
            }
        }
    }

    public void UnregisterPlayerController(string username)
    {
        if (_playerControllers.ContainsKey(username))
        {
            if (_currentPlayerCamera != null && _playerControllers[username].PlayerCamera == _currentPlayerCamera)
            {
                SwitchToMainCamera();
            }
            _playerControllers.Remove(username);
        }
    }

    public void SwitchToMainCamera()
    {
        _mainCamera.enabled = true;
        foreach (var playerController in _playerControllers.Values)
        {
            playerController.PlayerCamera.enabled = false;
        }
        _currentPlayerCamera = null;
    }

    public void SwitchToPlayerCamera(PlayerController playerController)
    {
        if (playerController == null)
        {
            Debug.LogWarning("[CameraManager] Attempted to switch to null PlayerController");
            return;
        }

        _mainCamera.enabled = false;

        foreach (var otherPlayerController in _playerControllers.Values)
        {
            if (otherPlayerController != playerController)
            {
                otherPlayerController.PlayerCamera.enabled = false;
            }
        }

        playerController.PlayerCamera.enabled = true;
        _currentPlayerCamera = playerController.PlayerCamera;
    }

    private void SetMainCameraActive()
    {
        _mainCamera.enabled = true;
        foreach (var playerController in _playerControllers.Values)
        {
            playerController.PlayerCamera.enabled = false;
        }
        _currentPlayerCamera = null;
    }

    private void OnPlayerAuthenticated()
    {
        Player currentPlayer = _serverController.GetCurrentPlayer();
        if (currentPlayer != null && _playerControllers.ContainsKey(currentPlayer.Username))
        {
            SwitchToPlayerCamera(_playerControllers[currentPlayer.Username]);
        }
    }

    private void OnSessionDisconnect()
    {
        SwitchToMainCamera();
    }

}
