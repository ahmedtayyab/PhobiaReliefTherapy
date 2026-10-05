using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace PhobiaReliefTherapy.Data
{
    /// <summary>
    /// Device-local accounts so login, admin, and tests work even if Supabase is unavailable.
    /// </summary>
    public static class LocalAccountStore
    {
        public const string CloudAdminId = "11111111-1111-1111-1111-111111111111";
        public const string CloudUserId = "22222222-2222-2222-2222-222222222222";
        public const string ActiveStatus = "active";
        public const string SuspendedStatus = "suspended";

        [Serializable]
        public class Account
        {
            public string Id;
            public string Username;
            public string Email;
            public string PasswordHash;
            public bool IsAdmin;
            public string AccountStatus = ActiveStatus;
            public string CreatedAt;
        }

        [Serializable]
        public class AuditEntry
        {
            public string Timestamp;
            public string AdminUserId;
            public string Action;
            public string TargetUserId;
            public string Details;
        }

        [Serializable]
        private class StoreFile
        {
            public List<Account> accounts = new List<Account>();
            public List<AuditEntry> auditLog = new List<AuditEntry>();
        }

        private static StoreFile cache;

        private static string StorePath => Path.Combine(Application.persistentDataPath, "PhobiaReliefTherapy_accounts.json");

        public static Account SeedAdmin => FindByEmail("admin@phobia.local");
        public static Account SeedUser => FindByEmail("user@phobia.local");

        public static List<Account> GetAllAccounts()
        {
            return Load().accounts;
        }

        public static List<AuditEntry> GetAuditLog()
        {
            var copy = new List<AuditEntry>(Load().auditLog);
            copy.Reverse();
            return copy;
        }

        public static Account FindByEmail(string email)
        {
            if (string.IsNullOrEmpty(email))
                return null;

            return Load().accounts.Find(a =>
                !string.IsNullOrEmpty(a.Email) &&
                a.Email.Equals(email.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        public static Account FindById(string id)
        {
            if (string.IsNullOrEmpty(id))
                return null;

            return Load().accounts.Find(a => a.Id == id);
        }

        public static Account TryLogin(string email, string password, out string error)
        {
            EnsureSeedAccounts();
            error = null;

            var account = FindByEmail(email);
            if (account == null)
            {
                error = "No account found for that email.";
                return null;
            }

            if (!HashesMatch(account.PasswordHash, password))
            {
                error = "Invalid email or password.";
                return null;
            }

            if (string.Equals(account.AccountStatus, SuspendedStatus, StringComparison.OrdinalIgnoreCase))
            {
                error = "This account is suspended. Contact an administrator.";
                return null;
            }

            return account;
        }

        public static Account Register(string username, string email, string password, bool isAdmin, out string error)
        {
            EnsureSeedAccounts();
            error = null;

            if (FindByEmail(email) != null)
            {
                error = "Email already registered.";
                return null;
            }

            var account = new Account
            {
                Id = Guid.NewGuid().ToString(),
                Username = username,
                Email = email.Trim(),
                PasswordHash = HashPassword(password),
                IsAdmin = isAdmin,
                AccountStatus = ActiveStatus,
                CreatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };

            var store = Load();
            store.accounts.Add(account);
            Save(store);
            return account;
        }

        public static void UpsertFromCloud(string id, string username, string email, bool isAdmin)
        {
            if (string.IsNullOrEmpty(email) && string.IsNullOrEmpty(id))
                return;

            EnsureSeedAccounts();
            var store = Load();
            var existing = store.accounts.Find(a =>
                (!string.IsNullOrEmpty(id) && a.Id == id) ||
                (!string.IsNullOrEmpty(email) && a.Email.Equals(email, StringComparison.OrdinalIgnoreCase)));

            if (existing == null)
            {
                store.accounts.Add(new Account
                {
                    Id = string.IsNullOrEmpty(id) ? Guid.NewGuid().ToString() : id,
                    Username = username,
                    Email = email,
                    PasswordHash = "",
                    IsAdmin = isAdmin,
                    AccountStatus = ActiveStatus,
                    CreatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                });
            }
            else
            {
                if (!string.IsNullOrEmpty(id))
                    existing.Id = id;
                if (!string.IsNullOrEmpty(username))
                    existing.Username = username;
                if (!string.IsNullOrEmpty(email))
                    existing.Email = email;
                existing.IsAdmin = existing.IsAdmin || isAdmin;
            }

            Save(store);
        }

        public static bool UpdateProfile(string userId, string username, string email, out string error)
        {
            error = null;
            var store = Load();
            var account = store.accounts.Find(a => a.Id == userId);
            if (account == null)
            {
                error = "Account not found.";
                return false;
            }

            var clash = store.accounts.Find(a =>
                a.Id != userId &&
                !string.IsNullOrEmpty(email) &&
                a.Email.Equals(email.Trim(), StringComparison.OrdinalIgnoreCase));
            if (clash != null)
            {
                error = "Email already registered.";
                return false;
            }

            if (!string.IsNullOrEmpty(username))
                account.Username = username.Trim();
            if (!string.IsNullOrEmpty(email))
                account.Email = email.Trim();

            Save(store);
            return true;
        }

        public static bool ChangePassword(string userId, string newPassword, out string error)
        {
            error = null;
            if (string.IsNullOrEmpty(newPassword) || newPassword.Length < 8)
            {
                error = "Password must be at least 8 characters.";
                return false;
            }

            var store = Load();
            var account = store.accounts.Find(a => a.Id == userId);
            if (account == null)
            {
                error = "Account not found.";
                return false;
            }

            account.PasswordHash = HashPassword(newPassword);
            Save(store);
            return true;
        }

        public static bool SetStatus(string adminUserId, string targetUserId, string status, out string error)
        {
            error = null;
            if (adminUserId == targetUserId)
            {
                error = "You cannot change the status of your own account.";
                return false;
            }

            var store = Load();
            var account = store.accounts.Find(a => a.Id == targetUserId);
            if (account == null)
            {
                error = "Account not found.";
                return false;
            }

            account.AccountStatus = status;
            store.auditLog.Add(NewAudit(adminUserId, status == SuspendedStatus ? "Suspend" : "Activate", targetUserId, $"status={status}"));
            Save(store);
            return true;
        }

        public static bool DeleteAccount(string adminUserId, string targetUserId, out string error)
        {
            error = null;
            if (adminUserId == targetUserId)
            {
                error = "You cannot delete your own account.";
                return false;
            }

            var store = Load();
            var account = store.accounts.Find(a => a.Id == targetUserId);
            if (account == null)
            {
                error = "Account not found.";
                return false;
            }

            store.accounts.Remove(account);
            store.auditLog.Add(NewAudit(adminUserId, "Delete", targetUserId, account.Email));
            Save(store);
            return true;
        }

        public static void LogAdminAction(string adminUserId, string action, string targetUserId, string details)
        {
            var store = Load();
            store.auditLog.Add(NewAudit(adminUserId, action, targetUserId, details));
            Save(store);
        }

        public static void EnsureSeedAccounts()
        {
            var store = Load();
            bool changed = false;

            if (store.accounts.Find(a => a.Email == "admin@phobia.local") == null)
            {
                store.accounts.Add(new Account
                {
                    Id = CloudAdminId,
                    Username = "admin",
                    Email = "admin@phobia.local",
                    PasswordHash = HashPassword("admin123"),
                    IsAdmin = true,
                    AccountStatus = ActiveStatus,
                    CreatedAt = "2026-01-01 00:00:00"
                });
                changed = true;
            }
            else
            {
                var admin = store.accounts.Find(a => a.Email == "admin@phobia.local");
                admin.Id = CloudAdminId;
                admin.PasswordHash = HashPassword("admin123");
                admin.IsAdmin = true;
                admin.AccountStatus = ActiveStatus;
                changed = true;
            }

            if (store.accounts.Find(a => a.Email == "user@phobia.local") == null)
            {
                store.accounts.Add(new Account
                {
                    Id = CloudUserId,
                    Username = "demo_user",
                    Email = "user@phobia.local",
                    PasswordHash = HashPassword("user1234"),
                    IsAdmin = false,
                    AccountStatus = ActiveStatus,
                    CreatedAt = "2026-01-01 00:00:00"
                });
                changed = true;
            }
            else
            {
                var user = store.accounts.Find(a => a.Email == "user@phobia.local");
                user.Id = CloudUserId;
                user.PasswordHash = HashPassword("user1234");
                user.AccountStatus = ActiveStatus;
                changed = true;
            }

            if (changed)
                Save(store);
        }

        private static AuditEntry NewAudit(string adminUserId, string action, string targetUserId, string details)
        {
            return new AuditEntry
            {
                Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                AdminUserId = adminUserId,
                Action = action,
                TargetUserId = targetUserId,
                Details = details
            };
        }

        private static StoreFile Load()
        {
            if (cache != null)
                return cache;

            if (!File.Exists(StorePath))
            {
                cache = new StoreFile();
                return cache;
            }

            try
            {
                cache = JsonUtility.FromJson<StoreFile>(File.ReadAllText(StorePath)) ?? new StoreFile();
                if (cache.accounts == null)
                    cache.accounts = new List<Account>();
                if (cache.auditLog == null)
                    cache.auditLog = new List<AuditEntry>();
                return cache;
            }
            catch (Exception ex)
            {
                Debug.LogError($"LocalAccountStore read failed: {ex.Message}");
                cache = new StoreFile();
                return cache;
            }
        }

        private static void Save(StoreFile store)
        {
            cache = store;
            try
            {
                File.WriteAllText(StorePath, JsonUtility.ToJson(store, true));
            }
            catch (Exception ex)
            {
                Debug.LogError($"LocalAccountStore write failed: {ex.Message}");
            }
        }

        public static string HashPassword(string password)
        {
            using (var sha = SHA256.Create())
            {
                byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(password ?? ""));
                var builder = new StringBuilder(bytes.Length * 2);
                foreach (byte b in bytes)
                    builder.Append(b.ToString("x2"));
                return builder.ToString();
            }
        }

        private static bool HashesMatch(string storedHash, string password)
        {
            if (string.IsNullOrEmpty(storedHash))
                return false;

            return string.Equals(storedHash, HashPassword(password), StringComparison.OrdinalIgnoreCase);
        }
    }
}
