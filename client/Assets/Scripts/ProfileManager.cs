using UnityEngine;
using UnityEngine.UI;

public class ProfileManager : MonoBehaviour
{
    [SerializeField]
    private ProfilePicturesScriptableObjectScript _profilePicturesScriptableObjectScript;
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
        _serverController.UpdateProfile(_currentProfileImage,
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
        Player player = _serverController.GetCurrentPlayer();
        _currentProfileImage = GetValidProfilePictureIndex(profileImage, player);
        _profilePictureImage.sprite = FindProfileImage(_currentProfileImage, player);
    }

    public void SetCurrentPlayerProfile(int profileImage)
    {
        SetLocalProfilePicture(profileImage);
    }

    private Sprite FindProfileImage(int index, Player player)
    {
        int validIndex = GetValidProfilePictureIndex(index, player);
        return _profilePicturesScriptableObjectScript.profilePictures[validIndex];
    }

    private int GetValidProfilePictureIndex(int index, Player player)
    {
        if (index == -1)
        {
            // Convert username to a deterministic integer for consistent "random" selection
            return Mathf.Abs(player.Username.GetHashCode()) % _profilePicturesScriptableObjectScript.profilePictures.Count;
        }
        else
        {
            return index % _profilePicturesScriptableObjectScript.profilePictures.Count;
        }
    }

    public Sprite FindProfileImage(Player player)
    {
        return FindProfileImage(player.Profile.ProfileImage, player);
    }

    private void OnProfileUpdate(string username, int profileImage)
    {
        _playerManager.UpdateProfile(username, new Profile(profileImage));

        // Check if this is the current player
        Player currentPlayer = _serverController.GetCurrentPlayer();
        bool isCurrentPlayer = currentPlayer != null && currentPlayer.Username == username;

        if (isCurrentPlayer)
        {
            _statusController.SetStatus("Profile updated");
            SetLocalProfilePicture(profileImage);
        }
        else
        {
            _statusController.SetStatus($"{username} updated their profile");
        }
    }
}
