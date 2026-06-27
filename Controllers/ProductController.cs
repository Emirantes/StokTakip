using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using StokTakip.Models;
using StokTakip.Security;
using StokTakip.Services;

namespace StokTakip.Controllers
{
    [Authorize]
    public class ProductController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ITenantContext _tenantContext;
        private readonly IProductVisibilityService _productVisibility;
        private readonly IAuditLogService _auditLogService;

        public ProductController(
            AppDbContext context,
            ITenantContext tenantContext,
            IProductVisibilityService productVisibility,
            IAuditLogService auditLogService)
        {
            _context = context;
            _tenantContext = tenantContext;
            _productVisibility = productVisibility;
            _auditLogService = auditLogService;
        }

        public async Task<IActionResult> Index(
            string? searchString,
            int? kategoriId)
        {
            IQueryable<Product> products =
                _productVisibility.Apply(
                    _context.Products
                        .AsNoTracking()
                        .Include(x => x.Kategori)
                        .Where(x => x.IsActive));

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                products = products.Where(x => x.Name.Contains(searchString));
            }

            if (kategoriId.HasValue)
            {
                products = products.Where(x => x.KategoriId == kategoriId);
            }

            await SetCategorySelectList(kategoriId);

            return View(await products
                .OrderByDescending(x => x.CreatedDate)
                .ToListAsync());
        }

        [Authorize(Roles = RoleNames.Owner + "," + RoleNames.Admin + "," + RoleNames.Personel)]
        public async Task<IActionResult> Create()
        {
            await SetCategorySelectList();

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = RoleNames.Owner + "," + RoleNames.Admin + "," + RoleNames.Personel)]
        public async Task<IActionResult> Create(Product product)
        {
            int userId = RequireUserId();
            int companyId = RequireCompanyId();

            if (!ModelState.IsValid)
            {
                await SetCategorySelectList(product.KategoriId);

                return View(product);
            }

            product.UserId = userId;
            product.CreatedByUserId = userId;
            product.CompanyId = companyId;
            product.CreatedDate = DateTime.Now;
            product.IsActive = true;

            _context.Products.Add(product);
            _auditLogService.Add($"{product.Name} isimli ürün eklendi.");

            await _context.SaveChangesAsync();

            TempData["Success"] = "Ürün başarıyla eklendi.";

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Details(int id)
        {
            var product =
                await _productVisibility.Apply(
                    _context.Products
                        .AsNoTracking()
                        .Include(x => x.Kategori))
                .FirstOrDefaultAsync(x => x.Id == id);

            if (product == null)
            {
                TempData["Error"] = "Ürün bulunamadı.";

                return RedirectToAction(nameof(Index));
            }

            return View(product);
        }

        [Authorize(Roles = RoleNames.Owner + "," + RoleNames.Admin + "," + RoleNames.Personel)]
        public async Task<IActionResult> Edit(int id)
        {
            var product =
                await _productVisibility.Apply(_context.Products)
                    .FirstOrDefaultAsync(x => x.Id == id);

            if (product == null)
            {
                TempData["Error"] = "Ürün bulunamadı.";

                return RedirectToAction(nameof(Index));
            }

            await SetCategorySelectList(product.KategoriId);

            return View(product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = RoleNames.Owner + "," + RoleNames.Admin + "," + RoleNames.Personel)]
        public async Task<IActionResult> Edit(Product product)
        {
            if (!ModelState.IsValid)
            {
                await SetCategorySelectList(product.KategoriId);

                return View(product);
            }

            var existingProduct =
                await _productVisibility.Apply(_context.Products)
                    .FirstOrDefaultAsync(x => x.Id == product.Id);

            if (existingProduct == null)
            {
                TempData["Error"] = "Ürün bulunamadı.";

                return RedirectToAction(nameof(Index));
            }

            existingProduct.Name = product.Name;
            existingProduct.Price = product.Price;
            existingProduct.Stock = product.Stock;
            existingProduct.KategoriId = product.KategoriId;
            existingProduct.CriticalStock = product.CriticalStock;
            existingProduct.StockType = product.StockType;

            _auditLogService.Add($"{product.Name} ürünü güncellendi.");
            await _context.SaveChangesAsync();

            TempData["Success"] = "Ürün başarıyla güncellendi.";

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = RoleNames.Owner + "," + RoleNames.Admin + "," + RoleNames.Personel)]
        public async Task<IActionResult> Delete(int id)
        {
            var product =
                await _productVisibility.Apply(_context.Products)
                    .FirstOrDefaultAsync(x => x.Id == id);

            if (product == null)
            {
                TempData["Error"] = "Ürün bulunamadı.";

                return RedirectToAction(nameof(Index));
            }

            product.IsActive = false;

            _auditLogService.Add($"{product.Name} ürünü silindi.");
            await _context.SaveChangesAsync();

            TempData["Success"] = "Ürün başarıyla silindi.";

            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = RoleNames.Owner + "," + RoleNames.Admin)]
        public async Task<IActionResult> StokDurumu()
        {
            var kritikUrunler =
                await _context.Products
                     .Where(x =>
                     x.IsActive &&
                     x.CompanyId == RequireCompanyId() &&
                     (x.Stock ?? 0) <= x.CriticalStock)
                    .OrderBy(x => x.Stock)
                    .ToListAsync();

            return View(kritikUrunler);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = RoleNames.Owner + "," + RoleNames.Admin + "," + RoleNames.Personel)]
        public async Task<IActionResult> StockAction(
            int id,
            int quantity,
            string actionType)
        {
            var product =
                await _productVisibility.Apply(_context.Products)
                    .FirstOrDefaultAsync(x => x.Id == id);

            if (product == null)
            {
                TempData["Error"] = "Ürün bulunamadı.";

                return RedirectToAction(nameof(Index));
            }

            quantity = Math.Max(quantity, 1);

            if (actionType == "add")
            {
                product.Stock = (product.Stock ?? 0) + quantity;
            }
            else if (actionType == "remove")
            {
                if ((product.Stock ?? 0) < quantity)
                {
                    TempData["Error"] = "Yetersiz stok.";

                    return RedirectToAction(nameof(Index));
                }

                product.Stock = (product.Stock ?? 0) - quantity;
            }
            else
            {
                TempData["Error"] = "Geçersiz stok işlemi.";

                return RedirectToAction(nameof(Index));
            }

            _context.StockMovements.Add(new StockMovement
            {
                ProductId = product.Id,
                Quantity = quantity,
                MovementType = actionType,
                CompanyId = RequireCompanyId(),
                UserId = RequireUserId(),
                Username =
                    HttpContext.Session.GetString("Username") ??
                    User.Identity?.Name,
                CreatedDate = DateTime.Now
            });

            string movementText =
                actionType == "add" ? "stok artırıldı" : "stok azaltıldı";

            _auditLogService.Add($"{product.Name} için {movementText}. Miktar: {quantity}");
            await _context.SaveChangesAsync();

            TempData["Success"] = "Stok başarıyla güncellendi.";

            return RedirectToAction(nameof(Index));
        }

        private async Task SetCategorySelectList(int? selectedId = null)
        {
            ViewBag.KategoriListesi =
                new SelectList(
                    await _context.Kategoriler
                        .AsNoTracking()
                        .Where(x => x.IsActive && x.CompanyId == RequireCompanyId())
                        .ToListAsync(),
                    "Id",
                    "KategoriAd",
                    selectedId);

            ViewBag.Kategoriler = ViewBag.KategoriListesi;
        }

        private int RequireCompanyId()
        {
            return _tenantContext.CompanyId ??
                throw new InvalidOperationException("Şirket oturumu bulunamadı.");
        }

        private int RequireUserId()
        {
            return _tenantContext.UserId ??
                throw new InvalidOperationException("Kullanıcı oturumu bulunamadı.");
        }
    }
}
