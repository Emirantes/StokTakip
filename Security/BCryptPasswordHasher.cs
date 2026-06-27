using Microsoft.AspNetCore.Identity;
using StokTakip.Models;

namespace StokTakip.Security
{
    public sealed class BCryptPasswordHasher : IPasswordHasher<User>
    {
        private const int WorkFactor = 12;

        public string HashPassword(
            User user,
            string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(
                password,
                WorkFactor);
        }

        public PasswordVerificationResult VerifyHashedPassword(
            User user,
            string hashedPassword,
            string providedPassword)
        {
            if (string.IsNullOrWhiteSpace(hashedPassword) ||
                string.IsNullOrEmpty(providedPassword))
            {
                return PasswordVerificationResult.Failed;
            }

            bool verified =
                BCrypt.Net.BCrypt.Verify(
                    providedPassword,
                    hashedPassword);

            return verified
                ? PasswordVerificationResult.Success
                : PasswordVerificationResult.Failed;
        }
    }
}
