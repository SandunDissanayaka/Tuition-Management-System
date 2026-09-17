using System;
using System.Security.Cryptography;
using System.Text;

namespace TuitionManagementSystem.Helpers
{
    /// <summary>Turns plain-text passwords into a SHA-256 hash so raw passwords are
    /// never stored in the SQLite database, keeping things simple but not insecure.</summary>
    public static class PasswordHelper
    {
        public static string Hash(string plainTextPassword)
        {
            using var sha256 = SHA256.Create();
            byte[] bytes = Encoding.UTF8.GetBytes(plainTextPassword);
            byte[] hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }

        public static bool Verify(string plainTextPassword, string storedHash)
        {
            return Hash(plainTextPassword) == storedHash;
        }
    }
}
