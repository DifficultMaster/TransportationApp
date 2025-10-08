using System;
using System.Security.Cryptography;
using System.Text;

namespace AppServer.Data
{
    public static class PasswordHandler
    {
        public static string GetHashedPassword(string password)
        {
            if (string.IsNullOrEmpty(password))
                throw new ArgumentException("Password cannot be empty");

            using var sha256 = SHA256.Create();
            byte[] bytes = Encoding.UTF8.GetBytes(password);
            byte[] hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }

        public static bool VerifyPassword(string password, string encodedPassword)
        {
            if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(encodedPassword))
                return false;

            string computedHash = GetHashedPassword(password);
            return computedHash == encodedPassword;
        }
    }
}