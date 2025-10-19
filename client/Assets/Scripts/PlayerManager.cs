using System.Collections.Generic;
using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    [SerializeField]
    private GameObject _playerPrefab;

    private Dictionary<string, PlayerController> _playerControllers = new Dictionary<string, PlayerController>();
    private ServerController _serverController;

    public void Start()
    {
        _serverController = ServiceLocator.Get<ServerController>();
        _serverController.OnLocationUpdate += OnLocationUpdate;
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
