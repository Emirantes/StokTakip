
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using StokTakip.Models;

namespace StokTakip.Controllers
{
    public class ProductController : Controller
    {
        private readonly AppDbContext _context;

        public ProductController(AppDbContext context)
        {
            _context = context;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            if (HttpContext.Session.GetInt32("UserId") == null)
            {
                context.Result = RedirectToAction("Login", "Account");
            }

            base.OnActionExecuting(context);
        }

        public async Task<IActionResult> Index(string searchString, int? kategoriId)
        {
            var products = _context.Products
                .Include(p => p.Kategori)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                products = products.Where(s =>
                    s.Name != null &&
                    s.Name.Contains(searchString));
            }

            if (kategoriId.HasValue)
            {
                products = products.Where(p => p.KategoriId == kategoriId);
            }

            ViewBag.Kategoriler =
                new SelectList(_context.Kategoriler, "Id", "KategoriAd");

            return View(await products.ToListAsync());
        }

        public IActionResult Create()
        {
            ViewBag.KategoriListesi =
                new SelectList(_context.Kategoriler, "Id", "KategoriAd");

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Product product)
        {
            if (ModelState.IsValid)
            {
                int? userId = HttpContext.Session.GetInt32("UserId");

                if (userId == null)
                {
                    return RedirectToAction("Login", "Account");
                }

                product.UserId = userId.Value;

                _context.Add(product);

                _context.Logs.Add(new Log
                {
                    Islem = $"{product.Name} isimli yeni ürün eklendi.",
                    Tarih = DateTime.Now
                });

                await _context.SaveChangesAsync();

                TempData["Success"] = "Ürün başarıyla eklendi.";

                return RedirectToAction(nameof(Index));
            }

            ViewBag.KategoriListesi =
                new SelectList(_context.Kategoriler, "Id", "KategoriAd");

            return View(product);
        }

        public async Task<IActionResult> Details(int id)
        {
            var product = await _context.Products
                .Include(p => p.Kategori)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
                return NotFound();

            return View(product);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var product = await _context.Products
            .FirstOrDefaultAsync(p =>
            p.Id == id &&
            p.UserId ==
            HttpContext.Session.GetInt32("UserId"));

            if (product == null)
                return NotFound();

            ViewBag.KategoriListesi =
                new SelectList(_context.Kategoriler, "Id", "KategoriAd");

            return View(product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Product product)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(product);

                    _context.Logs.Add(new Log
                    {
                        Islem = $"{product.Name} ürünü güncellendi.",
                        Tarih = DateTime.Now
                    });

                    await _context.SaveChangesAsync();

                    TempData["Success"] = "Ürün güncellendi.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Products.Any(e => e.Id == product.Id))
                        return NotFound();

                    else
                        throw;
                }

                return RedirectToAction(nameof(Index));
            }

            ViewBag.KategoriListesi =
                new SelectList(_context.Kategoriler, "Id", "KategoriAd");

            return View(product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var product = await _context.Products
            .FirstOrDefaultAsync(p =>
            p.Id == id &&
            p.UserId ==
            HttpContext.Session.GetInt32("UserId"));

            if (product != null)
            {
                _context.Products.Remove(product);

                _context.Logs.Add(new Log
                {
                    Islem = $"{product.Name} ürünü sistemden silindi.",
                    Tarih = DateTime.Now
                });

                await _context.SaveChangesAsync();

                TempData["Success"] =
                    "Ürün başarıyla silindi.";
            }
            else
            {
                TempData["Error"] =
                    "Ürün bulunamadı.";
            }

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> StokDurumu()
        {
            var kritikUrunler = await _context.Products
                .Include(p => p.Kategori)
                .Where(p => p.Stock < 10)
                .ToListAsync();

            return View(kritikUrunler);
        }
    }
}

