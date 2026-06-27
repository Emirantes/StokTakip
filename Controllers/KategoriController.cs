using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StokTakip.Models;
using StokTakip.Security;
using StokTakip.Services;

namespace StokTakip.Controllers
{
    [Authorize]
    public class KategoriController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ITenantContext _tenantContext;
        private readonly IAuditLogService _auditLogService;
        private readonly ILogger<KategoriController> _logger;

        public KategoriController(
            AppDbContext context,
            ITenantContext tenantContext,
            IAuditLogService auditLogService,
            ILogger<KategoriController> logger)
        {
            _context = context;
            _tenantContext = tenantContext;
            _auditLogService = auditLogService;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var kategoriler =
            await _context.Kategoriler
            .AsNoTracking()
            .Where(x =>
            x.IsActive &&
            x.CompanyId == RequireCompanyId())
            .OrderBy(x => x.KategoriAd)
            .ToListAsync();

            return View(kategoriler);
        }

        [Authorize(Roles = RoleNames.Owner + "," + RoleNames.Admin)]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = RoleNames.Owner + "," + RoleNames.Admin)]
        public async Task<IActionResult> Create(Kategori kategori)
        {
            if (!ModelState.IsValid)
            {
                return View(kategori);
            }

            bool kategoriVarMi =
                await CategoryNameExists(kategori.KategoriAd);

            if (kategoriVarMi)
            {
                TempData["Error"] = "Bu kategori zaten mevcut.";

                return View(kategori);
            }

            kategori.CompanyId = RequireCompanyId();
            kategori.CreatedDate = DateTime.Now;
            kategori.IsActive = true;

            _context.Kategoriler.Add(kategori);
            _auditLogService.Add($"{kategori.KategoriAd} kategorisi eklendi.");
            await _context.SaveChangesAsync();

            TempData["Success"] = "Kategori başarıyla oluşturuldu.";

            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = RoleNames.Owner + "," + RoleNames.Admin)]
        public async Task<IActionResult> Edit(int id)
        {
            var kategori =
                await _context.Kategoriler
                    .FirstOrDefaultAsync(x => x.Id == id);

            if (kategori == null)
            {
                TempData["Error"] = "Kategori bulunamadı.";

                return RedirectToAction(nameof(Index));
            }

            return View(kategori);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = RoleNames.Owner + "," + RoleNames.Admin)]
        public async Task<IActionResult> Edit(Kategori kategori)
        {
            if (!ModelState.IsValid)
            {
                return View(kategori);
            }

            var mevcutKategori =
                await _context.Kategoriler
                    .FirstOrDefaultAsync(x => x.Id == kategori.Id);

            if (mevcutKategori == null)
            {
                TempData["Error"] = "Kategori bulunamadı.";

                return RedirectToAction(nameof(Index));
            }

            bool ayniKategoriVarMi =
                await CategoryNameExists(kategori.KategoriAd, kategori.Id);

            if (ayniKategoriVarMi)
            {
                TempData["Error"] = "Bu kategori adı zaten kullanılıyor.";

                return View(kategori);
            }

            mevcutKategori.KategoriAd = kategori.KategoriAd.Trim();
            mevcutKategori.Aciklama = kategori.Aciklama?.Trim();
            mevcutKategori.IsActive = kategori.IsActive;

            _auditLogService.Add($"{kategori.KategoriAd} kategorisi güncellendi.");
            await _context.SaveChangesAsync();

            TempData["Success"] = "Kategori başarıyla güncellendi.";

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = RoleNames.Owner + "," + RoleNames.Admin)]
        public async Task<IActionResult> Delete(int id)
        {
            var kategori =
                await _context.Kategoriler
                    .FirstOrDefaultAsync(x => x.Id == id);

            if (kategori == null)
            {
                TempData["Error"] = "Kategori bulunamadı.";

                return RedirectToAction(nameof(Index));
            }

            bool hasProducts =
            await _context.Products
            .AnyAsync(p =>
            p.KategoriId == id &&
            p.IsActive);

            if (hasProducts)
            {
                TempData["Error"] =
                    "Bu kategoriye bağlı ürün bulunduğu için silinemez.";

                _auditLogService.Add(
                    $"{kategori.KategoriAd} kategorisi ürüne bağlı olduğu için silinemedi.");

                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }

            try
            {
                kategori.IsActive = false;

                _auditLogService.Add(
                $"{kategori.KategoriAd} kategorisi pasifleştirildi.");

                await _context.SaveChangesAsync();

                TempData["Success"] =
                    "Kategori başarıyla silindi.";
            }
            catch (DbUpdateException ex)
            {
                _logger.LogWarning(
                    ex,
                    "Kategori silinemedi. KategoriId: {KategoriId}, CompanyId: {CompanyId}",
                    id,
                    RequireCompanyId());

                TempData["Error"] =
                    "Bu kategoriye bağlı kayıt bulunduğu için silinemez.";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = RoleNames.Owner + "," + RoleNames.Admin)]
        public async Task<JsonResult> HizliEkle(
            string kategoriAd,
            string? aciklama)
        {
            if (string.IsNullOrWhiteSpace(kategoriAd))
            {
                return Json(new
                {
                    success = false,
                    message = "Kategori adı boş olamaz."
                });
            }

            kategoriAd = kategoriAd.Trim();

            if (await CategoryNameExists(kategoriAd))
            {
                return Json(new
                {
                    success = false,
                    message = "Bu kategori zaten mevcut."
                });
            }

            var yeniKategori = new Kategori
            {
                KategoriAd = kategoriAd,
                Aciklama = string.IsNullOrWhiteSpace(aciklama) ? null : aciklama.Trim(),
                CompanyId = RequireCompanyId(),
                CreatedDate = DateTime.Now,
                IsActive = true
            };

            _context.Kategoriler.Add(yeniKategori);
            _auditLogService.Add($"{yeniKategori.KategoriAd} kategorisi hızlı eklendi.");
            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                message = "Kategori başarıyla oluşturuldu.",
                id = yeniKategori.Id,
                ad = yeniKategori.KategoriAd
            });
        }

        private async Task<bool> CategoryNameExists(
            string kategoriAd,
            int? excludeId = null)
        {
            string normalizedName = kategoriAd.Trim().ToUpper();

            int companyId = RequireCompanyId();

            return await _context.Kategoriler
                .AsNoTracking()
                .AnyAsync(x =>
                    x.CompanyId == companyId &&
                    x.KategoriAd.ToUpper() == normalizedName &&
                    (!excludeId.HasValue || x.Id != excludeId.Value));
        }

        private int RequireCompanyId()
        {
            return _tenantContext.CompanyId ??
                throw new InvalidOperationException("Şirket oturumu bulunamadı.");
        }
    }
}
