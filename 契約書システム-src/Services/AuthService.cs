using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using ContractManager.Models;

namespace ContractManager.Services
{
    public static class AuthService
    {
        private static User? _currentUser;

        public static User? CurrentUser => _currentUser;

        public static bool IsLoggedIn => _currentUser != null;

        public static bool IsAdmin => _currentUser?.IsAdmin ?? false;

        public static (bool success, string message) Login(string username, string password)
        {
            var users = DataStore.LoadUsers();
            var user = users.FirstOrDefault(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));

            if (user == null)
            {
                return (false, "ユーザーが見つかりません");
            }

            var hash = HashPassword(password, user.Salt);
            if (hash != user.PasswordHash)
            {
                return (false, "パスワードが正しくありません");
            }

            _currentUser = user;
            return (true, "ログイン成功");
        }

        public static void Logout()
        {
            _currentUser = null;
        }

        public static string GenerateSalt()
        {
            var saltBytes = new byte[16];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(saltBytes);
            return Convert.ToBase64String(saltBytes);
        }

        public static string HashPassword(string password, string salt)
        {
            var saltBytes = Convert.FromBase64String(salt);
            using var pbkdf2 = new Rfc2898DeriveBytes(password, saltBytes, 300000, HashAlgorithmName.SHA256);
            var hashBytes = pbkdf2.GetBytes(32);
            return Convert.ToBase64String(hashBytes);
        }

        public static (bool success, string message) CreateAdminFromConsole(string username, string password)
        {
            var users = DataStore.LoadUsers();
            
            if (users.Any(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase)))
            {
                return (false, "このユーザー名は既に使用されています");
            }

            var salt = GenerateSalt();
            var hash = HashPassword(password, salt);

            var admin = new User
            {
                Username = username,
                PasswordHash = hash,
                Salt = salt,
                IsAdmin = true
            };

            users.Add(admin);
            DataStore.SaveUsers(users);

            return (true, "管理者アカウントを作成しました");
        }
    }
}
