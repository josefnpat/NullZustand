using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ChatManager : MonoBehaviour
{
    [SerializeField]
    private TMP_Text _chatText;
    [SerializeField]
    private TMP_InputField _chatInputField;
    [SerializeField]
    private Button _chatButton;

    private ServerController _serverController;

    void Start()
    {
        _serverController = ServiceLocator.Get<ServerController>();

        _serverController.OnNewChatMessage += OnNewChatMessage;
        _chatButton.onClick.AddListener(SendChatMessage);
    }

    private void OnNewChatMessage(string username, string message, long timestamp)
    {
        _chatText.text += $"{username}: {message}\n";
    }

    private void SendChatMessage()
    {
        _serverController.SendNewChatMessage(_chatInputField.text);
        _chatInputField.text = string.Empty;
    }

}
