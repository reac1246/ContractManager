using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using ContractManager.Models;

namespace ContractManager.Services
{
    public static class DataStore
    {
        private static readonly string BaseDirectory;
        private static readonly string AccountsDirectory;
        private static readonly string DocsDirectory;
        
        private static readonly string UsersFile;
        private static readonly string ContractsFile;
        private static readonly string AssignmentsFile;

        static DataStore()
        {
            // Get the application's base directory
            BaseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            
            // Navigate to the parent directory structure (契約書/)
            var parentDir = Directory.GetParent(BaseDirectory);
            while (parentDir != null && !Directory.Exists(Path.Combine(parentDir.FullName, "Accounts")))
            {
                parentDir = parentDir.Parent;
            }
            
            if (parentDir != null)
            {
                AccountsDirectory = Path.Combine(parentDir.FullName, "Accounts");
                DocsDirectory = Path.Combine(parentDir.FullName, "Doc");
            }
            else
            {
                // Fallback to current directory structure
                AccountsDirectory = Path.Combine(BaseDirectory, "..", "Accounts");
                DocsDirectory = Path.Combine(BaseDirectory, "..", "Doc");
            }
            
            // Ensure directories exist
            Directory.CreateDirectory(AccountsDirectory);
            Directory.CreateDirectory(DocsDirectory);
            
            // Set file paths
            UsersFile = Path.Combine(AccountsDirectory, "users.json");
            ContractsFile = Path.Combine(DocsDirectory, "contracts.json");
            AssignmentsFile = Path.Combine(AccountsDirectory, "assignments.json");
        }

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };

        // Users
        public static List<User> LoadUsers()
        {
            if (!File.Exists(UsersFile))
                return new List<User>();
            
            var json = File.ReadAllText(UsersFile);
            return JsonSerializer.Deserialize<List<User>>(json, JsonOptions) ?? new List<User>();
        }

        public static void SaveUsers(List<User> users)
        {
            var json = JsonSerializer.Serialize(users, JsonOptions);
            File.WriteAllText(UsersFile, json);
        }

        // Contracts
        public static List<Contract> LoadContracts()
        {
            if (!File.Exists(ContractsFile))
                return new List<Contract>();
            
            var json = File.ReadAllText(ContractsFile);
            return JsonSerializer.Deserialize<List<Contract>>(json, JsonOptions) ?? new List<Contract>();
        }

        public static void SaveContracts(List<Contract> contracts)
        {
            var json = JsonSerializer.Serialize(contracts, JsonOptions);
            File.WriteAllText(ContractsFile, json);
        }

        // Assignments
        public static List<ContractAssignment> LoadAssignments()
        {
            if (!File.Exists(AssignmentsFile))
                return new List<ContractAssignment>();
            
            var json = File.ReadAllText(AssignmentsFile);
            return JsonSerializer.Deserialize<List<ContractAssignment>>(json, JsonOptions) ?? new List<ContractAssignment>();
        }

        public static void SaveAssignments(List<ContractAssignment> assignments)
        {
            var json = JsonSerializer.Serialize(assignments, JsonOptions);
            File.WriteAllText(AssignmentsFile, json);
        }

        public static string GetAccountsDirectory() => AccountsDirectory;
        public static string GetDocsDirectory() => DocsDirectory;
    }
}
