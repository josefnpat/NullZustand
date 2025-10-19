using System;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;

namespace NullZustand
{
    public class UserAccount
    {
        public string Username { get; set; }
        public string PasswordHash { get; set; }
        public string Salt { get; set; }
        public DateTime CreatedAt { get; set; }

        public UserAccount(string username, string passwordHash, string salt)
        {
            Username = username;
            PasswordHash = passwordHash;
            Salt = salt;
            CreatedAt = DateTime.UtcNow;
        }
    }

    public class UserAccountManager
    {
        private readonly ConcurrentDictionary<string, UserAccount> _accounts;
        private const int SALT_SIZE = 16; // 128 bits
        private const int HASH_SIZE = 32; // 256 bits
        private const int ITERATIONS = 10000; // PBKDF2 iterations

        public UserAccountManager()
        {
            _accounts = new ConcurrentDictionary<string, UserAccount>(StringComparer.OrdinalIgnoreCase);
        }

        private string GenerateSalt()
        {
            byte[] saltBytes = new byte[SALT_SIZE];
            using (var rng = new RNGCryptoServiceProvider())
            {
                rng.GetBytes(saltBytes);
            }
            return Convert.ToBase64String(saltBytes);
        }

        private string HashPassword(string password, string salt)
        {
            byte[] saltBytes = Convert.FromBase64String(salt);
            using (var pbkdf2 = new Rfc2898DeriveBytes(password, saltBytes, ITERATIONS))
            {
                byte[] hashBytes = pbkdf2.GetBytes(HASH_SIZE);
                return Convert.ToBase64String(hashBytes);
            }
        }

        private bool VerifyPassword(string password, string hash, string salt)
        {
            string computedHash = HashPassword(password, salt);
            return computedHash == hash;
        }

        public bool RegisterUser(string username, string password, out string error)
        {
            error = null;

            // Validate username
            if (string.IsNullOrWhiteSpace(username))
            {
                error = "Username cannot be empty";
                return false;
            }

            if (username.Length < ValidationConstants.MIN_USERNAME_LENGTH)
            {
                error = $"Username must be at least {ValidationConstants.MIN_USERNAME_LENGTH} characters long";
                return false;
            }

            if (username.Length > ValidationConstants.MAX_USERNAME_LENGTH)
            {
                error = $"Username must be at most {ValidationConstants.MAX_USERNAME_LENGTH} characters long";
                return false;
            }

            // Validate password
            if (string.IsNullOrWhiteSpace(password))
            {
                error = "Password cannot be empty";
                return false;
            }

            // Check if user already exists
            if (_accounts.ContainsKey(username))
            {
                error = "Username already exists";
                return false;
            }

            // Hash the password
            string salt = GenerateSalt();
            string passwordHash = HashPassword(password, salt);

            // Create new account with hashed password
            var account = new UserAccount(username, passwordHash, salt);
            if (_accounts.TryAdd(username, account))
            {
                Console.WriteLine($"[ACCOUNT] New user registered: {username} (Total users: {_accounts.Count})");
                return true;
            }

            error = "Failed to create account";
            return false;
        }

        public bool ValidateCredentials(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                return false;
            }

            if (_accounts.TryGetValue(username, out UserAccount account))
            {
                return VerifyPassword(password, account.PasswordHash, account.Salt);
            }

            return false;
        }

        public bool UserExists(string username)
        {
            return !string.IsNullOrWhiteSpace(username) && _accounts.ContainsKey(username);
        }

        public int GetUserCount()
        {
            return _accounts.Count;
        }
    }
}

