using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ProfileManager : MonoBehaviour
{
    [SerializeField]
    private ProfilePicturesScriptableObjectScript _profilePicturesScriptableObjectScript;
    [SerializeField]
    private TMP_InputField _displayNameInputField;
    [SerializeField]
    private Button _nextProfilePictureButton;
    [SerializeField]
    private Button _previousProfilePictureButton;
    [SerializeField]
    private Image _profilePictureImage;
    [SerializeField]
    private Button _updateProfileButton;

    private ServerController _serverController;
    private PlayerManager _playerManager;
    private StatusController _statusController;

    private int _currentProfileImage = -1;

    void Awake()
    {
        ServiceLocator.Register<ProfileManager>(this);
    }

    void Start()
    {
        _serverController = ServiceLocator.Get<ServerController>();
        _playerManager = ServiceLocator.Get<PlayerManager>();
        _statusController = ServiceLocator.Get<StatusController>();

        _previousProfilePictureButton.onClick.AddListener(OnPreviousProfilePictureButton);
        _nextProfilePictureButton.onClick.AddListener(OnNextProfilePictureButton);
        _updateProfileButton.onClick.AddListener(OnUpdateProfileButton);

        _serverController.OnProfileUpdate += OnProfileUpdate;
    }

    private void OnPreviousProfilePictureButton()
    {
        if (_currentProfileImage > 0)
        {
            SetLocalProfilePicture(_currentProfileImage - 1);
        }
    }

    private void OnNextProfilePictureButton()
    {
        if (_currentProfileImage < _profilePicturesScriptableObjectScript.profilePictures.Count - 1)
        {
            SetLocalProfilePicture(_currentProfileImage + 1);
        }
    }

    private void OnUpdateProfileButton()
    {
        _serverController.UpdateProfile(_displayNameInputField.text, _currentProfileImage,
            OnUpdateProfileSuccess, OnUpdateProfileFailure);
    }

    private void OnUpdateProfileSuccess(object payload)
    {
        _statusController.SetStatus("Profile updated successfully!");
    }

    private void OnUpdateProfileFailure(string error)
    {
        _statusController.SetStatus($"Profile update failed: {error}");
    }

    public void SetLocalProfilePicture(int profileImage)
    {
        if (profileImage == -1)
        {
            Player currentPlayer = _serverController.GetCurrentPlayer();
            // Convert username to a deterministic integer for consistent "random" selection
            _currentProfileImage = Mathf.Abs(currentPlayer.Username.GetHashCode());
        }
        else
        {
            _currentProfileImage = profileImage;
        }
        _currentProfileImage %= _profilePicturesScriptableObjectScript.profilePictures.Count;
        _profilePictureImage.sprite = FindProfileImage(_currentProfileImage);
    }

    public void SetProfile(string displayName, int profileImage)
    {
        _displayNameInputField.text = displayName;
        SetLocalProfilePicture(profileImage);
    }

    private Sprite FindProfileImage(int index)
    {
        return _profilePicturesScriptableObjectScript.profilePictures[index];
    }

    private void OnProfileUpdate(string username, string displayName, int profileImage)
    {
        _playerManager.SetProfile(username, new Profile(displayName, profileImage));

        // Check if this is the current player
        Player currentPlayer = _serverController.GetCurrentPlayer();
        bool isCurrentPlayer = currentPlayer != null && currentPlayer.Username == username;

        if (isCurrentPlayer)
        {
            _statusController.SetStatus($"Profile updated: {displayName}");
            _displayNameInputField.text = displayName;
            SetLocalProfilePicture(profileImage);
        }
        else
        {
            _statusController.SetStatus($"{username} updated their profile");
        }
    }
}
