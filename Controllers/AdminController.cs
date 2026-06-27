using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StokTakip.Models;
using StokTakip.Security;

namespace StokTakip.Controllers
{
    [Authorize(Roles = RoleNames.Owner)]
    public class AdminController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly ITenantContext _tenantContext;

        public AdminController(
            AppDbContext context,
            UserManager<User> userManager,
            ITenantContext tenantContext)
        {
            _context = context;
            _userManager = userManager;
            _tenantContext = tenantContext;
        }

        public async Task<IActionResult> Index()
        {
            int companyId = RequireCompanyId();

            var company =
                await _context.Companies
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == companyId);

            var users =
                await _context.Users
                    .AsNoTracking()
                    .Where(x =>
                        x.CompanyId == companyId &&
                        x.IsActive)
                    .OrderByDescending(x => x.Id)
                    .ToListAsync();

            ViewBag.ToplamKullanici = users.Count;

            ViewBag.ToplamUrun =
            await _context.Products
            .CountAsync(x =>
            x.CompanyId == companyId &&
            x.IsActive);

            ViewBag.ToplamKategori =
            await _context.Kategoriler
            .CountAsync(x =>
            x.CompanyId == companyId &&
            x.IsActive);

            ViewBag.ToplamOwner =
                users.Count(x => x.Role == RoleNames.Owner);

            ViewBag.CompanyCode =
            company?.PlanType == PlanType.Personal.ToString()
            ? "-"
            : company?.CompanyCode ?? "-";

            ViewBag.PlanType =
            company?.PlanType ??
            PlanType.Team.ToString();

            return View(users);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeRole(
            int id,
            string role)
        {
            int companyId = RequireCompanyId();

            var company =
                await _context.Companies
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == companyId);

            if (company?.PlanType == PlanType.Personal.ToString())
            {
                TempData["Error"] =
                    "Personal planda kullanıcı rol yönetimi kapalıdır.";

                return RedirectToAction(nameof(Index));
            }

            if (!IsValidRoleForPlan(role, company?.PlanType))
            {
                TempData["Error"] = "Geçersiz rol seçildi.";

                return RedirectToAction(nameof(Index));
            }

            var user =
                await _context.Users
                    .FirstOrDefaultAsync(x =>
                        x.Id == id &&
                        x.CompanyId == companyId &&
                        x.IsActive);

            if (user == null)
            {
                TempData["Error"] = "Kullanıcı bulunamadı.";

                return RedirectToAction(nameof(Index));
            }

            if (user.Id == RequireUserId() &&
                role != RoleNames.Owner)
            {
                TempData["Error"] =
                    "Kendi Owner rolünüzü değiştiremezsiniz.";

                return RedirectToAction(nameof(Index));
            }

            if (user.Role == RoleNames.Owner &&
                role != RoleNames.Owner)
            {
                int ownerCount =
                    await _context.Users
                        .CountAsync(x =>
                            x.CompanyId == companyId &&
                            x.IsActive &&
                            x.Role == RoleNames.Owner);

                if (ownerCount <= 1)
                {
                    TempData["Error"] =
                        "Şirkette en az bir Owner olmalıdır.";

                    return RedirectToAction(nameof(Index));
                }
            }

            IdentityResult roleResult =
                await ReplaceIdentityRole(user, role);

            if (!roleResult.Succeeded)
            {
                TempData["Error"] =
                    "Kullanıcı rolü güncellenemedi. Lütfen tekrar deneyin.";

                return RedirectToAction(nameof(Index));
            }

            user.Role = role;

            await AddLog($"{user.Username} kullanıcısının rolü {role} yapıldı.");
            await _context.SaveChangesAsync();

            TempData["Success"] = "Kullanıcı rolü güncellendi.";

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(int id)
        {
            int companyId = RequireCompanyId();
            int currentUserId = RequireUserId();

            var user =
                await _context.Users
                    .FirstOrDefaultAsync(x =>
                        x.Id == id &&
                        x.CompanyId == companyId &&
                        x.IsActive);

            if (user == null)
            {
                TempData["Error"] = "Kullanıcı bulunamadı.";

                return RedirectToAction(nameof(Index));
            }

            if (user.Id == currentUserId)
            {
                TempData["Error"] = "Kendi hesabınızı silemezsiniz.";

                return RedirectToAction(nameof(Index));
            }

            if (user.Role == RoleNames.Owner)
            {
                int ownerCount =
                    await _context.Users
                        .CountAsync(x =>
                            x.CompanyId == companyId &&
                            x.IsActive &&
                            x.Role == RoleNames.Owner);

                if (ownerCount <= 1)
                {
                    TempData["Error"] =
                        "Şirkette en az bir Owner olmalıdır.";

                    return RedirectToAction(nameof(Index));
                }
            }

            user.IsActive = false;

            await AddLog($"{user.Username} kullanıcısı pasifleştirildi.");
            await _context.SaveChangesAsync();

            TempData["Success"] = "Kullanıcı silindi.";

            return RedirectToAction(nameof(Index));
        }

        private static bool IsValidRoleForPlan(
            string role,
            string? planType)
        {
            return role is
                RoleNames.Owner or
                RoleNames.Admin or
                RoleNames.Personel;
        }

        private async Task<IdentityResult> ReplaceIdentityRole(
            User user,
            string role)
        {
            var currentRoles =
                await _userManager.GetRolesAsync(user);

            if (currentRoles.Count > 0)
            {
                IdentityResult removeResult =
                    await _userManager.RemoveFromRolesAsync(user, currentRoles);

                if (!removeResult.Succeeded)
                {
                    return removeResult;
                }
            }

            return await _userManager.AddToRoleAsync(user, role);
        }

        private async Task AddLog(string message)
        {
            _context.Logs.Add(new Log
            {
                Islem = message,
                Tarih = DateTime.Now,
                CompanyId = RequireCompanyId(),
                UserId = _tenantContext.UserId,
                KullaniciAdi = HttpContext.Session.GetString("Username"),
                Rol = HttpContext.Session.GetString("Role"),
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
            });

            await Task.CompletedTask;
        }

        private int RequireCompanyId()
        {
            return _tenantContext.CompanyId ??
                throw new InvalidOperationException("Company context is missing.");
        }

        private int RequireUserId()
        {
            return _tenantContext.UserId ??
                throw new InvalidOperationException("User context is missing.");
        }
    }
}
