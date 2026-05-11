using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using StokTakip.Models;

namespace StokTakip.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppDbContext _context;

        public HomeController(AppDbContext context)
        {
            _context = context;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            if (HttpContext.Session.GetInt32("UserId") == null)
            {
                context.Result =
                    RedirectToAction("Login", "Account");
            }

            base.OnActionExecuting(context);
        }

        public async Task<IActionResult> Index()
        {
            int? userId =
                HttpContext.Session.GetInt32("UserId");

            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            ViewBag.ToplamUrun =
                await _context.Products
                    .CountAsync(p => p.UserId == userId);

            ViewBag.KategoriSayisi =
                await _context.Kategoriler.CountAsync();

            ViewBag.KritikStok =
                await _context.Products
                    .CountAsync(p =>
                        p.UserId == userId &&
                        p.Stock != null &&
                        p.Stock < 10);

            ViewBag.SonIslemler =
                await _context.Logs
                    .OrderByDescending(l => l.Tarih)
                    .Take(5)
                    .ToListAsync();

            return View();
        }
    }
}