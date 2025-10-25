using System;

public class Profile
{
    public string DisplayName { get; set; }
    public int ProfileImage { get; set; }

    public Profile(string username)
    {
        DisplayName = username ?? throw new ArgumentNullException(nameof(username));
        ProfileImage = -1;
    }

    public Profile(string displayName, int profileImage)
    {
        DisplayName = displayName ?? throw new ArgumentNullException(nameof(displayName));
        ProfileImage = profileImage;
    }
}
