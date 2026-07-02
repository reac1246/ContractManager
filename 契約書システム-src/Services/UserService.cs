using System;
using System.Collections.Generic;
using System.Linq;
using ContractManager.Models;

namespace ContractManager.Services
{
    public static class UserService
    {
        public static List<User> GetAllUsers()
        {
            return DataStore.LoadUsers();
        }

        public static List<User> GetNonAdminUsers()
        {
            return DataStore.LoadUsers().Where(u => !u.IsAdmin).ToList();
        }

        public static User? GetUserById(string id)
        {
            return DataStore.LoadUsers().FirstOrDefault(u => u.Id == id);
        }

        public static User? GetUserByUsername(string username)
        {
            return DataStore.LoadUsers().FirstOrDefault(u => 
                u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
        }

        public static (bool success, string message) CreateUser(string username, string password, string nameEnglish = "", DateTime? birthDate = null)
        {
            var users = DataStore.LoadUsers();
            
            if (users.Any(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase)))
            {
                return (false, "このユーザー名は既に使用されています");
            }

            var salt = AuthService.GenerateSalt();
            var hash = AuthService.HashPassword(password, salt);

            var user = new User
            {
                Username = username,
                PasswordHash = hash,
                Salt = salt,
                IsAdmin = false,
                NameEnglish = nameEnglish,
                BirthDate = birthDate
            };

            users.Add(user);
            DataStore.SaveUsers(users);

            return (true, "ユーザーを作成しました");
        }

        public static (bool success, string message) UpdateUser(string id, string? username = null, string? password = null, string? nameEnglish = null, DateTime? birthDate = null)
        {
            var users = DataStore.LoadUsers();
            var user = users.FirstOrDefault(u => u.Id == id);

            if (user == null)
            {
                return (false, "ユーザーが見つかりません");
            }

            if (username != null && !username.Equals(user.Username, StringComparison.OrdinalIgnoreCase))
            {
                if (users.Any(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase)))
                {
                    return (false, "このユーザー名は既に使用されています");
                }
                user.Username = username;
            }

            if (password != null)
            {
                var salt = AuthService.GenerateSalt();
                user.Salt = salt;
                user.PasswordHash = AuthService.HashPassword(password, salt);
            }

            if (nameEnglish != null)
            {
                user.NameEnglish = nameEnglish;
            }

            if (birthDate.HasValue)
            {
                user.BirthDate = birthDate;
            }

            DataStore.SaveUsers(users);
            return (true, "ユーザーを更新しました");
        }

        public static (bool success, string message) DeleteUser(string id)
        {
            var users = DataStore.LoadUsers();
            var user = users.FirstOrDefault(u => u.Id == id);

            if (user == null)
            {
                return (false, "ユーザーが見つかりません");
            }

            // Also delete all assignments for this user
            var assignments = DataStore.LoadAssignments();
            assignments.RemoveAll(a => a.UserId == id);
            DataStore.SaveAssignments(assignments);

            users.Remove(user);
            DataStore.SaveUsers(users);

            return (true, "ユーザーを削除しました");
        }
    }
}
