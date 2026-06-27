using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StokTakip.Models;
using StokTakip.Security;
using StokTakip.Services;

namespace StokTakip.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ITenantContext _tenantContext;
        private readonly IProductVisibilityService _productVisibility;

        public HomeController(
            AppDbContext context,
            ITenantContext tenantContext,
            IProductVisibilityService productVisibility)
        {
            _context = context;
            _tenantContext = tenantContext;
            _productVisibility = productVisibility;
        }

        public async Task<IActionResult> Index()
        {
            int userId =
                _tenantContext.UserId ??
                throw new InvalidOperationException("Kullanıcı oturumu bulunamadı.");

            int companyId =
                _tenantContext.CompanyId ??
                throw new InvalidOperationException("Şirket oturumu bulunamadı.");

            IQueryable<Product> products =
                _productVisibility.Apply(
                    _context.Products
                        .AsNoTracking()
                        .Where(x => x.IsActive));

            var productStats =
                await products
                    .GroupBy(_ => 1)
                    .Select(g => new
                    {
                        TotalProduct = g.Count(),
                        CriticalStock =
                            g.Count(x => (x.Stock ?? 0) <= x.CriticalStock),
                        TotalStockValue =
                            g.Sum(x => (x.Price ?? 0) * (x.Stock ?? 0))
                    })
                    .FirstOrDefaultAsync();

            int totalCategory =
                await _context.Kategoriler
                    .AsNoTracking()
                    .CountAsync(x => x.IsActive &&
                    x.CompanyId == companyId);

            IQueryable<Log> latestLogQuery =
                _context.Logs.AsNoTracking();

            if (_tenantContext.Role == RoleNames.Personel)
            {
                latestLogQuery = latestLogQuery.Where(x => x.UserId == userId);
            }

            var latestLogs =
                await latestLogQuery
                    .OrderByDescending(x => x.Tarih)
                    .Take(10)
                    .ToListAsync();

            Company? company =
                await _context.Companies
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == companyId);

            ViewBag.ToplamUrun = productStats?.TotalProduct ?? 0;
            ViewBag.KategoriSayisi = totalCategory;
            ViewBag.KritikStok = productStats?.CriticalStock ?? 0;
            ViewBag.ToplamStokDegeri = productStats?.TotalStockValue ?? 0;
            ViewBag.SonIslemler = latestLogs;
            ViewBag.CompanyCode =
                company?.PlanType == PlanType.Personal.ToString()
                    ? "-"
                    : company?.CompanyCode ?? "-";
            ViewBag.CompanyName = company?.CompanyName ?? "-";

            return View();
        }

        [AllowAnonymous]
        public IActionResult Error()
        {
            return View();
        }
    }
}
