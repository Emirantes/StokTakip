using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StokTakip.Models;
using System.Security.Cryptography;
using System.Text;

namespace StokTakip.Controllers
{
    public class AccountController : Controller
    {
        private readonly AppDbContext _context;

        public AccountController(AppDbContext context)
        {
            _context = context;
        }

        // LOGIN SAYFASI
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        // LOGIN İŞLEMİ
        [HttpPost]
        public async Task<IActionResult> Login(string username, string password)
        {
            string hashedPassword = HashPassword(password);

            var user = await _context.Users.FirstOrDefaultAsync(
                u => u.Username == username &&
                     u.Password == hashedPassword);

            if (user != null)
            {
                HttpContext.Session.SetInt32("UserId", user.Id);
                HttpContext.Session.SetString("Username", user.Username);

                return RedirectToAction("Index", "Home");
            }

            ViewBag.Error = "Kullanıcı adı veya şifre hatalı!";
            return View();
        }

        // REGISTER SAYFASI
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        // REGISTER İŞLEMİ
        [HttpPost]
        public async Task<IActionResult> Register(User newUser)
        {
            if (ModelState.IsValid)
            {
                bool usernameExists = await _context.Users
                .AnyAsync(x => x.Username == newUser.Username);

                if (usernameExists)
                {
                    ViewBag.Error = "Bu kullanıcı adı zaten mevcut.";
                    return View(newUser);
                }

                bool emailExists = await _context.Users
                    .AnyAsync(x => x.Email == newUser.Email);

                if (emailExists)
                {
                    ViewBag.Error = "Bu e-posta adresi zaten kayıtlı.";
                    return View(newUser);
                }

                newUser.Password = HashPassword(newUser.Password);

                _context.Users.Add(newUser);

                await _context.SaveChangesAsync();

                TempData["Success"] =
                    "Kayıt başarılı. Giriş yapabilirsiniz.";

                return RedirectToAction("Login");
            }

            return View(newUser);
        }

        // PROFİL
        public async Task<IActionResult> Profile()
        {
            int? userId = HttpContext.Session.GetInt32("UserId");

            if (userId == null)
                return RedirectToAction("Login");

            var user = await _context.Users.FindAsync(userId);

            return View(user);
        }

        // PROFİL GÜNCELLE
        [HttpPost]
        public async Task<IActionResult> UpdateProfile(User updatedUser)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");

            if (userId == null)
                return RedirectToAction("Login");

            var user = await _context.Users.FindAsync(userId);

            if (user != null)
            {
                user.FullName = updatedUser.FullName;
                user.Username = updatedUser.Username;
                user.Email = updatedUser.Email;

                // Şifre boş değilse güncelle
                if (!string.IsNullOrWhiteSpace(updatedUser.Password))
                {
                    user.Password = HashPassword(updatedUser.Password);
                }

                _context.Update(user);

                await _context.SaveChangesAsync();

                // Session username güncelle
                HttpContext.Session.SetString(
                    "Username",
                    user.Username);

                TempData["Success"] =
                    "Profil başarıyla güncellendi.";
            }

            return RedirectToAction("Profile");
        }

        // LOGOUT
        [HttpGet]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();

            return RedirectToAction("Login");
        }

        // SHA256 HASH
        private string HashPassword(string password)
        {
            using (SHA256 sha256Hash = SHA256.Create())
            {
                byte[] bytes =
                    sha256Hash.ComputeHash(
                        Encoding.UTF8.GetBytes(password));

                StringBuilder builder = new StringBuilder();

                foreach (var item in bytes)
                {
                    builder.Append(item.ToString("x2"));
                }

                return builder.ToString();
            }
        }
    }
}