using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StokTakip.Models;
using StokTakip.Security;

namespace StokTakip.Controllers
{
    [Authorize(Roles = RoleNames.Owner + "," + RoleNames.Admin)]
    public class LogController : Controller
    {
        private readonly AppDbContext _context;

        public LogController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var logs =
                await _context.Logs
                    .AsNoTracking()
                    .OrderByDescending(x => x.Tarih)
                    .Take(500)
                    .ToListAsync();

            return View(logs);
        }
    }
}
