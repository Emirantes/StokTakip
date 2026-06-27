using Microsoft.AspNetCore.Identity;

namespace StokTakip.Security
{
    public sealed class TurkishIdentityErrorDescriber : IdentityErrorDescriber
    {
        public override IdentityError DuplicateEmail(string email) =>
            Error(nameof(DuplicateEmail), "Bu e-posta adresi zaten kullanılıyor.");

        public override IdentityError DuplicateUserName(string userName) =>
            Error(nameof(DuplicateUserName), "Bu kullanıcı adı zaten kullanılıyor.");

        public override IdentityError InvalidEmail(string? email) =>
            Error(nameof(InvalidEmail), "Geçerli bir e-posta adresi giriniz.");

        public override IdentityError InvalidUserName(string? userName) =>
            Error(nameof(InvalidUserName), "Geçerli bir kullanıcı adı giriniz.");

        public override IdentityError PasswordRequiresDigit() =>
            Error(nameof(PasswordRequiresDigit), "Şifre en az bir rakam içermelidir.");

        public override IdentityError PasswordRequiresLower() =>
            Error(nameof(PasswordRequiresLower), "Şifre en az bir küçük harf içermelidir.");

        public override IdentityError PasswordRequiresNonAlphanumeric() =>
            Error(nameof(PasswordRequiresNonAlphanumeric), "Şifre en az bir özel karakter içermelidir.");

        public override IdentityError PasswordRequiresUpper() =>
            Error(nameof(PasswordRequiresUpper), "Şifre en az bir büyük harf içermelidir.");

        public override IdentityError PasswordTooShort(int length) =>
            Error(nameof(PasswordTooShort), $"Şifre en az {length} karakter olmalıdır.");

        private static IdentityError Error(string code, string description) =>
            new()
            {
                Code = code,
                Description = description
            };
    }
}
