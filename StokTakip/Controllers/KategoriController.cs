using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using StokTakip.Models;

namespace StokTakip.Controllers
{
    public class KategoriController : Controller
    {
        private readonly AppDbContext _context;

        public KategoriController(AppDbContext context)
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

        public async Task<IActionResult> Index()
        {
            return View(await _context.Kategoriler.ToListAsync());
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(Kategori kategori)
        {
            if (ModelState.IsValid)
            {
                _context.Add(kategori);

                await _context.SaveChangesAsync();

                TempData["Success"] = "Kategori eklendi.";

                return RedirectToAction(nameof(Index));
            }

            return View(kategori);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var kategori = await _context.Kategoriler.FindAsync(id);

            if (kategori == null)
                return NotFound();

            return View(kategori);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(Kategori kategori)
        {
            if (ModelState.IsValid)
            {
                _context.Update(kategori);

                await _context.SaveChangesAsync();

                TempData["Success"] = "Kategori güncellendi.";

                return RedirectToAction(nameof(Index));
            }

            return View(kategori);
        }

        public async Task<IActionResult> Delete(int id)
        {
            var kategori = await _context.Kategoriler.FindAsync(id);

            if (kategori == null)
                return RedirectToAction(nameof(Index));

            var hasProducts =
                await _context.Products.AnyAsync(p => p.KategoriId == id);

            if (hasProducts)
            {
                TempData["Error"] =
                    "Bu kategoriye ait ürünler olduğu için silinemez.";

                return RedirectToAction(nameof(Index));
            }

            _context.Kategoriler.Remove(kategori);

            await _context.SaveChangesAsync();

            TempData["Success"] = "Kategori silindi.";

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<JsonResult> HizliEkle(string kategoriAd, string? aciklama)
        {
            if (string.IsNullOrEmpty(kategoriAd))
            {
                return Json(new
                {
                    success = false,
                    message = "Kategori adı boş olamaz."
                });
            }

            var yeniKategori = new Kategori
            {
                KategoriAd = kategoriAd,
                Aciklama = aciklama ?? "Genel Kategori"
            };

            _context.Kategoriler.Add(yeniKategori);

            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                id = yeniKategori.Id,
                ad = yeniKategori.KategoriAd
            });
        }
    }
}
