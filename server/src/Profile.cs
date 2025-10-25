using System;

namespace NullZustand
{
    public class Profile
    {
        public int ProfileImage { get; set; }

        public Profile(string username)
        {
            ProfileImage = -1;
        }

        public Profile(int profileImage)
        {
            ProfileImage = profileImage;
        }
    }
}
