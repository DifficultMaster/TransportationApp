using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using AppServer.Models;
using Microsoft.EntityFrameworkCore;

namespace AppServer.Data
{
    public class PasswordHandler
    {
        private readonly AppDbContext _context;

        public PasswordHandler(AppDbContext context)
        {
            _context = context;
        }

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

        public async Task<bool> ChangePasswordAsync(string personId, string oldPassword, string newPassword)
        {
            var person = await _context.Persons
                .Include(p => p.PasswordHistories)
                .FirstOrDefaultAsync(p => p.PersonId == personId);

            if (person == null || !VerifyPassword(oldPassword, person.HashedPassword))
            {
                return false;
            }

            var newHashedPassword = GetHashedPassword(newPassword);

            var recentPasswords = person.PasswordHistories
                .OrderByDescending(ph => ph.DateChanged)
                .Take(5)
                .Select(ph => ph.HashedPassword)
                .ToList();

            if (recentPasswords.Contains(newHashedPassword))
            {
                return false; 
            }

            person.HashedPassword = newHashedPassword;

            var newPasswordHistory = new PasswordHistory
            {
                PersonId = person.PersonId,
                HashedPassword = newHashedPassword,
                DateChanged = DateTime.UtcNow
            };
            _context.PasswordHistories.Add(newPasswordHistory);

            if (person.PasswordHistories.Count > 5)
            {
                var oldestPassword = person.PasswordHistories.OrderBy(ph => ph.DateChanged).First();
                _context.PasswordHistories.Remove(oldestPassword);
            }

            await _context.SaveChangesAsync();

            return true;
        }
    }
}