using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using StokTakip.Models;
using StokTakip.Security;

namespace StokTakip.Controllers
{
    public class AccountController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;

        public AccountController(
            AppDbContext context,
            UserManager<User> userManager,
            SignInManager<User> signInManager)
        {
            _context = context;
            _userManager = userManager;
            _signInManager = signInManager;
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> Login()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                if (HttpContext.Session.GetInt32("UserId") != null)
                {
                    return RedirectToAction("Index", "Home");
                }

                await SignOutAndClearSession();
            }

            return View();
        }

        [AllowAnonymous]
        [HttpPost]
        [EnableRateLimiting("LoginPolicy")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(
            string username,
            string password)
        {
            if (string.IsNullOrWhiteSpace(username) ||
                string.IsNullOrWhiteSpace(password))
            {
                TempData["Error"] = "Tüm alanları doldurun.";

                return View();
            }

            username = username.Trim();

            var user =
            await _context.Users
            .IgnoreQueryFilters()
            .Include(x => x.Company)
            .FirstOrDefaultAsync(x =>
            x.UserName != null &&
            x.UserName.ToLower() == username.ToLower());

            if (user == null)
            {
                TempData["Error"] =
                    "Kullanıcı adı veya şifre hatalı.";

                return View();
            }

            if (!user.IsActive)
            {
                TempData["Error"] =
                    "Hesabınız pasif durumda.";

                return View();
            }

            if (await _userManager.IsLockedOutAsync(user))
            {
                TempData["Error"] =
                    "Çok fazla başarısız deneme yapıldı. Lütfen daha sonra tekrar deneyin.";

                return View();
            }

            var result =
                await _signInManager.PasswordSignInAsync(
                    user,
                    password,
                    isPersistent: false,
                    lockoutOnFailure: true);

            if (!result.Succeeded)
            {
                TempData["Error"] =
                    "Kullanıcı adı veya şifre hatalı.";

                return View();
            }

            if (user.Company == null)
            {
                await _signInManager.SignOutAsync();

                TempData["Error"] =
                    "Şirket bilgisi bulunamadı.";

                return View();
            }

            SetSession(user);

            _context.Logs.Add(new Log
            {
                Islem =
                    $"{user.Username} sisteme giriş yaptı.",

                Tarih = DateTime.Now,
                CompanyId = user.CompanyId,
                UserId = user.Id,
                KullaniciAdi = user.Username,
                Rol = user.Role,
                IpAddress =
                    HttpContext.Connection.RemoteIpAddress?.ToString()
            });

            await _context.SaveChangesAsync();

            TempData["Success"] = "Giriş başarılı.";

            return RedirectToAction("Index", "Home");
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(
            User newUser,
            string? inviteCode,
            string systemType,
            string registerType)
        {
            newUser.Username = newUser.Username?.Trim() ?? "";
            newUser.Email = newUser.Email?.Trim() ?? "";
            newUser.UserName = newUser.Username;

            if (string.IsNullOrWhiteSpace(newUser.FullName) ||
                string.IsNullOrWhiteSpace(newUser.Username) ||
                string.IsNullOrWhiteSpace(newUser.Email) ||
                string.IsNullOrWhiteSpace(newUser.Password))
            {
                TempData["Error"] =
                    "Tüm alanları doldurun.";

                return View(newUser);
            }

            bool usernameExists =
                await _context.Users
                    .AnyAsync(x =>
                        x.UserName != null &&
                        x.UserName.ToLower() ==
                        newUser.Username.ToLower());

            if (usernameExists)
            {
                TempData["Error"] =
                    "Bu kullanıcı adı zaten kullanılıyor.";

                return View(newUser);
            }

            bool emailExists =
                await _context.Users
                    .AnyAsync(x =>
                        x.Email != null &&
                        x.Email.ToLower() ==
                        newUser.Email.ToLower());

            if (emailExists)
            {
                TempData["Error"] =
                    "Bu email adresi zaten kullanılıyor.";

                return View(newUser);
            }

            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            Company? company = null;

            if (systemType == PlanType.Personal.ToString())
            {
                company = new Company
                {
                    CompanyName = $"{newUser.Username} Personal",
                    CompanyCode = CreateCompanyCode(8),
                    CreatedDate = DateTime.Now,
                    PlanType = PlanType.Personal.ToString()
                };

                _context.Companies.Add(company);
                await _context.SaveChangesAsync();

                newUser.CompanyId = company.Id;
                newUser.Role = RoleNames.Owner;
            }
            else if (registerType == "invite")
            {
                if (string.IsNullOrWhiteSpace(inviteCode))
                {
                    TempData["Error"] =
                        "Davet kodu giriniz.";

                    return View(newUser);
                }

                inviteCode = inviteCode.Trim().ToUpperInvariant();

                company =
                    await _context.Companies
                        .FirstOrDefaultAsync(x =>
                            x.CompanyCode == inviteCode);

                if (company == null ||
                    company.PlanType == PlanType.Personal.ToString())
                {
                    TempData["Error"] =
                        "Geçersiz davet kodu.";

                    return View(newUser);
                }

                newUser.CompanyId = company.Id;
                newUser.Role = RoleNames.Personel;
            }
            else
            {
                company = new Company
                {
                    CompanyName = $"{newUser.Username} Şirketi",
                    CompanyCode = CreateCompanyCode(6),
                    CreatedDate = DateTime.Now,
                    PlanType =
                        systemType == PlanType.Enterprise.ToString()
                            ? PlanType.Enterprise.ToString()
                            : PlanType.Team.ToString()
                };

                _context.Companies.Add(company);
                await _context.SaveChangesAsync();

                newUser.CompanyId = company.Id;
                newUser.Role = RoleNames.Owner;
            }

            newUser.IsActive = true;
            newUser.CreatedDate = DateTime.Now;
            newUser.EmailConfirmed = true;

            IdentityResult result =
                await _userManager.CreateAsync(
                    newUser,
                    newUser.Password);

            if (!result.Succeeded)
            {
                await transaction.RollbackAsync();

                TempData["Error"] =
                    "Kayıt oluşturulamadı. Bilgilerinizi kontrol edip tekrar deneyin.";

                return View(newUser);
            }

            IdentityResult roleResult =
                await _userManager.AddToRoleAsync(
                    newUser,
                    newUser.Role);

            if (!roleResult.Succeeded)
            {
                await transaction.RollbackAsync();

                TempData["Error"] =
                    "Kullanıcı rolü atanamadı. Lütfen tekrar deneyin.";

                return View(newUser);
            }

            _context.Logs.Add(new Log
            {
                Islem =
                    $"{newUser.Username} sisteme kayıt oldu.",

                Tarih = DateTime.Now,
                CompanyId = newUser.CompanyId,
                UserId = newUser.Id,
                KullaniciAdi = newUser.Username,
                Rol = newUser.Role,
                IpAddress =
                    HttpContext.Connection.RemoteIpAddress?.ToString()
            });

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            TempData["Success"] = "Kayıt başarılı.";

            return RedirectToAction("Login");
        }

        public async Task<IActionResult> Profile()
        {
            int? userId =
                HttpContext.Session.GetInt32("UserId");

            int? companyId =
                HttpContext.Session.GetInt32("CompanyId");

            if (userId == null || companyId == null)
            {
                return RedirectToAction("Login");
            }

            var user =
                await _context.Users
                    .Include(x => x.Company)
                    .FirstOrDefaultAsync(x =>
                        x.Id == userId &&
                        x.CompanyId == companyId);

            if (user == null)
            {
                await SignOutAndClearSession();

                return RedirectToAction("Login");
            }

            user.Password = string.Empty;

            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(
            User updatedUser)
        {
            int? userId =
                HttpContext.Session.GetInt32("UserId");

            if (userId == null)
            {
                return RedirectToAction("Login");
            }

            int? companyId =
            HttpContext.Session.GetInt32("CompanyId");

            var user =
                await _context.Users
                    .FirstOrDefaultAsync(x =>
                        x.Id == userId &&
                        x.CompanyId == companyId);

            if (user == null)
            {
                await SignOutAndClearSession();

                return RedirectToAction("Login");
            }

            updatedUser.Username =
                updatedUser.Username?.Trim() ?? "";

            updatedUser.Email =
                updatedUser.Email?.Trim() ?? "";

            bool usernameExists =
                await _context.Users
                    .AnyAsync(x =>
                        x.UserName != null &&
                        x.UserName.ToLower() ==
                        updatedUser.Username.ToLower() &&
                        x.Id != user.Id);

            if (usernameExists)
            {
                TempData["Error"] =
                    "Bu kullanıcı adı başka biri tarafından kullanılıyor.";

                return RedirectToAction("Profile");
            }

            bool emailExists =
                await _context.Users
                    .AnyAsync(x =>
                        x.Email != null &&
                        x.Email.ToLower() ==
                        updatedUser.Email.ToLower() &&
                        x.Id != user.Id);

            if (emailExists)
            {
                TempData["Error"] =
                    "Bu email başka biri tarafından kullanılıyor.";

                return RedirectToAction("Profile");
            }

            user.FullName = updatedUser.FullName;
            user.Username = updatedUser.Username;
            user.UserName = updatedUser.Username;
            user.Email = updatedUser.Email;

            IdentityResult updateResult =
                await _userManager.UpdateAsync(user);

            if (!updateResult.Succeeded)
            {
                TempData["Error"] =
                    "Profil güncellenemedi. Lütfen bilgilerinizi kontrol edin.";

                return RedirectToAction("Profile");
            }

            if (!string.IsNullOrWhiteSpace(updatedUser.Password))
            {
                IdentityResult removeResult =
                    await _userManager.RemovePasswordAsync(user);

                if (!removeResult.Succeeded)
                {
                    TempData["Error"] =
                        "Şifre güncellenemedi. Lütfen tekrar deneyin.";

                    return RedirectToAction("Profile");
                }

                IdentityResult addResult =
                    await _userManager.AddPasswordAsync(
                        user,
                        updatedUser.Password);

                if (!addResult.Succeeded)
                {
                    TempData["Error"] =
                        "Yeni şifre kaydedilemedi. Lütfen şifre kurallarını kontrol edin.";

                    return RedirectToAction("Profile");
                }
            }

            await _signInManager.RefreshSignInAsync(user);
            SetSession(user);

            TempData["Success"] = "Profil güncellendi.";

            return RedirectToAction("Profile");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            _context.Logs.Add(new Log
            {
                Islem =
                    $"{HttpContext.Session.GetString("Username")} sistemden çıkış yaptı.",

                Tarih = DateTime.Now,
                CompanyId =
                    HttpContext.Session.GetInt32("CompanyId") ?? 0,
                UserId =
                    HttpContext.Session.GetInt32("UserId"),
                KullaniciAdi =
                    HttpContext.Session.GetString("Username"),
                Rol =
                    HttpContext.Session.GetString("Role"),
                IpAddress =
                    HttpContext.Connection.RemoteIpAddress?.ToString()
            });

            await _context.SaveChangesAsync();
            await SignOutAndClearSession();

            TempData["Success"] = "Çıkış yapıldı.";

            return RedirectToAction("Login");
        }

        private void SetSession(User user)
        {
            HttpContext.Session.SetInt32("UserId", user.Id);
            HttpContext.Session.SetString("Username", user.Username);
            HttpContext.Session.SetString("Role", user.Role);
            HttpContext.Session.SetInt32("CompanyId", user.CompanyId);
        }

        private async Task SignOutAndClearSession()
        {
            await _signInManager.SignOutAsync();
            HttpContext.Session.Clear();
        }

        private static string CreateCompanyCode(int length)
        {
            return Guid.NewGuid()
                .ToString("N")
                .Substring(0, length)
                .ToUpperInvariant();
        }
    }
}
