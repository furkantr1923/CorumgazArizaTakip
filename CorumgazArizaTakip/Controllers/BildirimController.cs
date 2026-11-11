using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CorumgazArizaTakip.Models;

namespace CorumgazArizaTakip.Controllers
{
    [AllowAnonymous]
    public class BildirimController : Controller
    {
        private const double AcikIsCezasiKm = 2.0;
        private readonly ArizaDbContext _context;

        public BildirimController(ArizaDbContext context)
        {
            _context = context;
        }

        private (Teknisyen? teknisyen, double mesafe) EnUygunTeknisyen(double enlem, double boylam)
        {
            var acikIsler = _context.Arizalar
                .Where(a => a.TeknisyenId != null && a.Durum != "Çözüldü")
                .GroupBy(a => a.TeknisyenId)
                .Select(g => new { TeknisyenId = g.Key, Sayi = g.Count() })
                .ToList()
                .ToDictionary(x => x.TeknisyenId!.Value, x => x.Sayi);

            var aday = _context.Teknisyenler
                .Where(t => t.Musait && t.Enlem != null && t.Boylam != null)
                .ToList()
                .Select(t =>
                {
                    double mesafe = ArizaController.MesafeKm(enlem, boylam, t.Enlem!.Value, t.Boylam!.Value);
                    int isSayisi = acikIsler.TryGetValue(t.Id, out var s) ? s : 0;
                    return new { Teknisyen = t, Mesafe = mesafe, Puan = mesafe + isSayisi * AcikIsCezasiKm };
                })
                .OrderBy(x => x.Puan)
                .FirstOrDefault();

            return aday == null ? (null, 0) : (aday.Teknisyen, aday.Mesafe);
        }

        public IActionResult Create()
        {
            return View(new Ariza());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create([Bind("MusteriAdi,Adres,ArizaTuru,Enlem,Boylam")] Ariza ariza, bool kvkkOnay)
        {
            if (ariza.Enlem == null || ariza.Boylam == null)
            {
                ModelState.AddModelError(string.Empty, "Lütfen haritadan arızanın konumunu seç.");
            }

            if (!kvkkOnay)
            {
                ModelState.AddModelError(string.Empty, "Devam etmek için KVKK aydınlatma metnini onaylaman gerekiyor.");
            }

            if (!ModelState.IsValid)
            {
                return View(ariza);
            }

            ariza.Oncelik = ariza.ArizaTuru.Contains("koku", StringComparison.OrdinalIgnoreCase)
                         || ariza.ArizaTuru.Contains("kaçak", StringComparison.OrdinalIgnoreCase)
                         || ariza.ArizaTuru.Contains("sızıntı", StringComparison.OrdinalIgnoreCase)
                ? "Acil"
                : "Normal";
            ariza.Durum = "Bekliyor";
            ariza.BildirimTarihi = DateTime.Now;
            ariza.KvkkOnayTarihi = DateTime.Now;
            ariza.TakipKodu = Ariza.YeniTakipKodu();

            var (teknisyen, mesafe) = EnUygunTeknisyen(ariza.Enlem!.Value, ariza.Boylam!.Value);
            if (teknisyen != null)
            {
                ariza.TeknisyenId = teknisyen.Id;
            }

            ariza.DurumGecmisleri.Add(new DurumGecmisi
            {
                EskiDurum = "-",
                YeniDurum = "Bekliyor",
                Degistiren = teknisyen != null ? $"Müşteri (web) · otomatik atama: {teknisyen.AdSoyad}" : "Müşteri (web)",
                Tarih = DateTime.Now
            });

            _context.Arizalar.Add(ariza);
            _context.SaveChanges();

            TempData["Kod"] = ariza.TakipKodu;
            TempData["Oncelik"] = ariza.Oncelik;
            if (teknisyen != null)
            {
                TempData["Teknisyen"] = teknisyen.AdSoyad;
                TempData["Mesafe"] = mesafe.ToString("F1");
            }

            return RedirectToAction("Sonuc");
        }

        public IActionResult Sonuc()
        {
            ViewBag.Kod = TempData["Kod"] as string;
            ViewBag.Oncelik = TempData["Oncelik"] as string;
            ViewBag.Teknisyen = TempData["Teknisyen"] as string;
            ViewBag.Mesafe = TempData["Mesafe"] as string;
            return View();
        }

        public IActionResult Sorgu(string? kod)
        {
            if (string.IsNullOrWhiteSpace(kod))
            {
                return View((Ariza?)null);
            }

            var temizKod = kod.Trim().ToUpper();
            var ariza = _context.Arizalar
                .Include(a => a.Teknisyen)
                .FirstOrDefault(a => a.TakipKodu == temizKod);

            ViewBag.Kod = kod;
            return View(ariza);
        }
    }
}