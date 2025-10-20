using UnityEngine;

public class PanelManager : MonoBehaviour
{
    private enum PanelState
    {
        Uninitialized,
        NotAuthenticated,
        Authenticated
    }

    [SerializeField]
    private GameObject _sessionPanel;
    [SerializeField]
    private GameObject _playerPanel;

    private PanelState _currentState = PanelState.Uninitialized;
    private ServerController _serverController;

    void Start()
    {
        _serverController = ServiceLocator.Get<ServerController>();

        _serverController.OnPlayerAuthenticate += OnPlayerAuthenticate;
        _serverController.OnSessionDisconnect += OnSessionDisconnect;

        TransitionToState(PanelState.NotAuthenticated);
    }

    private void OnPlayerAuthenticate()
    {
        TransitionToState(PanelState.Authenticated);
    }

    private void OnSessionDisconnect()
    {
        TransitionToState(PanelState.NotAuthenticated);
    }

    private void TransitionToState(PanelState newState)
    {
        if (_currentState != newState)
        {
            _currentState = newState;

            _sessionPanel.SetActive(false);
            _playerPanel.SetActive(false);

            if (_currentState == PanelState.NotAuthenticated)
            {
                _sessionPanel.SetActive(true);
            }
            else if (_currentState == PanelState.Authenticated)
            {
                _playerPanel.SetActive(true);
            }
        }
    }

    void OnDestroy()
    {
        if (_serverController != null)
        {
            _serverController.OnPlayerAuthenticate -= OnPlayerAuthenticate;
            _serverController.OnSessionDisconnect -= OnSessionDisconnect;
        }
    }
}
