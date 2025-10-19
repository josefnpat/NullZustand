using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SessionController : MonoBehaviour
{
    [SerializeField]
    private TMP_InputField _usernameInputField;
    [SerializeField]
    private TMP_InputField _passwordInputField;
    [SerializeField]
    private Button _loginButton;
    [SerializeField]
    private Button _registerButton;
    [SerializeField]
    private TMP_Text _statusText;
    [SerializeField]
    private Button _pingButton;
    [SerializeField]
    private TMP_InputField _xInputField;
    [SerializeField]
    private TMP_InputField _yInputField;
    [SerializeField]
    private TMP_InputField _zInputField;
    [SerializeField]
    private Button _updatePositionButton;
    [SerializeField]
    private Button _getLocationUpdatesButton;

    private ServerController _serverController;

    public void Start()
    {
        _serverController = ServiceLocator.Get<ServerController>();

        _loginButton.onClick.AddListener(OnLoginButtonPressed);
        _registerButton.onClick.AddListener(OnRegisterButtonPressed);
        _pingButton.onClick.AddListener(OnPingButtonPressed);
        _updatePositionButton.onClick.AddListener(OnUpdatePositionButtonPressed);
        _getLocationUpdatesButton.onClick.AddListener(OnGetLocationUpdatesButtonPressed);

        ClearStatus();
    }

    public void OnLoginButtonPressed()
    {
        string username = _usernameInputField.text;
        string password = _passwordInputField.text;
        ClearStatus();
        _serverController.Login(username, password);
    }

    public void OnRegisterButtonPressed()
    {
        string username = _usernameInputField.text;
        string password = _passwordInputField.text;
        ClearStatus();
        _serverController.Register(username, password);
    }

    public void OnPingButtonPressed()
    {
        ClearStatus();
        _serverController.SendPing();
    }

    public void OnUpdatePositionButtonPressed()
    {
        ClearStatus();
        _serverController.UpdatePosition(
            float.Parse(_xInputField.text),
            float.Parse(_yInputField.text),
            float.Parse(_zInputField.text)
        );
    }

    private void OnGetLocationUpdatesButtonPressed()
    {
        ClearStatus();
        _serverController.GetLocationUpdates();
    }

    private void ClearStatus()
    {
        _statusText.text = "";
    }

}
