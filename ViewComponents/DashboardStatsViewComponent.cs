using Microsoft.AspNetCore.Mvc;

namespace StokTakip.ViewComponents
{
    public class DashboardStatsViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke()
        {
 
            ViewBag.ToplamKategori = 12;
            ViewBag.ToplamUrun = 150;
            ViewBag.KritikStok = 3;

            return View();
        }
    }
}
