using System.Collections.Concurrent;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using CorumgazArizaTakip.Models;

namespace CorumgazArizaTakip.Controllers
{
    public class HesapController : Controller
    {
        private const int MaksimumDeneme = 5;
        private static readonly TimeSpan DenemePenceresi = TimeSpan.FromMinutes(15);
        private static readonly TimeSpan KilitSuresi = TimeSpan.FromMinutes(5);

        private static readonly ConcurrentDictionary<string, DenemeKaydi> Denemeler = new();
        private static readonly PasswordHasher<Kullanici> Hasher = new();
        private static readonly string SahteHash = Hasher.HashPassword(new Kullanici(), Guid.NewGuid().ToString());

        private readonly ArizaDbContext _context;

        public HesapController(ArizaDbContext context)
        {
            _context = context;
        }

        private record DenemeKaydi(int Sayi, DateTime IlkDeneme, DateTime? KilitBitis);

        private string Anahtar(string? kullaniciAdi)
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "bilinmiyor";
            return $"{(kullaniciAdi ?? "").Trim().ToLowerInvariant()}|{ip}";
        }

        private IActionResult GuvenliYonlendir(string? returnUrl)
        {
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction("Index", "Ariza");
        }

        public IActionResult Login(string? returnUrl)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return GuvenliYonlendir(returnUrl);
            }

            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string kullaniciAdi, string sifre, string? returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;
            ViewBag.KullaniciAdi = kullaniciAdi;

            var anahtar = Anahtar(kullaniciAdi);
            var simdi = DateTime.UtcNow;

            if (Denemeler.TryGetValue(anahtar, out var kayit) && kayit.KilitBitis > simdi)
            {
                var kalanDakika = Math.Ceiling((kayit.KilitBitis.Value - simdi).TotalMinutes);
                ViewBag.Hata = $"Çok fazla hatalı deneme yapıldı. {kalanDakika} dakika sonra tekrar dene.";
                return View();
            }

            var kullanici = _context.Kullanicilar.FirstOrDefault(k => k.KullaniciAdi == kullaniciAdi);

            var sonuc = kullanici == null
                ? Hasher.VerifyHashedPassword(new Kullanici(), SahteHash, sifre ?? "")
                : Hasher.VerifyHashedPassword(kullanici, kullanici.SifreHash, sifre ?? "");

            if (kullanici == null || sonuc == PasswordVerificationResult.Failed)
            {
                var yeni = kayit == null || simdi - kayit.IlkDeneme > DenemePenceresi
                    ? new DenemeKaydi(1, simdi, null)
                    : kayit with { Sayi = kayit.Sayi + 1 };

                if (yeni.Sayi >= MaksimumDeneme)
                {
                    yeni = yeni with { KilitBitis = simdi + KilitSuresi };
                    Denemeler[anahtar] = yeni;
                    ViewBag.Hata = $"Çok fazla hatalı deneme yapıldı. Giriş {KilitSuresi.TotalMinutes} dakika kilitlendi.";
                    return View();
                }

                Denemeler[anahtar] = yeni;
                ViewBag.Hata = $"Kullanıcı adı veya şifre hatalı. Kalan deneme hakkı: {MaksimumDeneme - yeni.Sayi}";
                return View();
            }

            Denemeler.TryRemove(anahtar, out _);

            if (sonuc == PasswordVerificationResult.SuccessRehashNeeded)
            {
                kullanici.SifreHash = Hasher.HashPassword(kullanici, sifre!);
                _context.SaveChanges();
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, kullanici.KullaniciAdi)
            };

            var kimlik = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(kimlik));

            return GuvenliYonlendir(returnUrl);
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }
    }
}