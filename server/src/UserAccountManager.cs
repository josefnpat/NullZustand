using System;
using System.Collections.Concurrent;

namespace NullZustand
{
    public class UserAccount
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public DateTime CreatedAt { get; set; }

        public UserAccount(string username, string password)
        {
            Username = username;
            Password = password;
            CreatedAt = DateTime.UtcNow;
        }
    }

    public class UserAccountManager
    {
        private readonly ConcurrentDictionary<string, UserAccount> _accounts;

        public UserAccountManager()
        {
            _accounts = new ConcurrentDictionary<string, UserAccount>(StringComparer.OrdinalIgnoreCase);
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

            if (username.Length < 3)
            {
                error = "Username must be at least 3 characters long";
                return false;
            }

            if (username.Length > 20)
            {
                error = "Username must be at most 20 characters long";
                return false;
            }

            // Validate password
            if (string.IsNullOrWhiteSpace(password))
            {
                error = "Password cannot be empty";
                return false;
            }

            if (password.Length < 6)
            {
                error = "Password must be at least 6 characters long";
                return false;
            }

            // Check if user already exists
            if (_accounts.ContainsKey(username))
            {
                error = "Username already exists";
                return false;
            }

            // Create new account
            var account = new UserAccount(username, password);
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
                // In a real application, you would use proper password hashing (e.g., bcrypt)
                // For in-memory storage as requested, we're storing plain text
                return account.Password == password;
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

