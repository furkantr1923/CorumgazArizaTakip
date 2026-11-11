using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using CorumgazArizaTakip.Models;

namespace CorumgazArizaTakip.Controllers
{
    [Authorize]
    public class ArizaController : Controller
    {
        private const int SayfaBoyutu = 10;
        private static readonly string[] GecerliDurumlar = { "Bekliyor", "Yolda", "Çözüldü" };
        private readonly ArizaDbContext _context;

        public ArizaController(ArizaDbContext context)
        {
            _context = context;
        }

        private string Personel => User?.Identity?.Name ?? "Personel";

        private void TeknisyenListesiHazirla(int? seciliId = null)
        {
            var liste = _context.Teknisyenler
                .OrderBy(t => t.AdSoyad)
                .ToList()
                .Select(t => new
                {
                    t.Id,
                    Ad = $"{t.AdSoyad} ({t.Bolge}) {(t.Musait ? "· Müsait" : "· Görevde")}"
                });

            ViewBag.Teknisyenler = new SelectList(liste, "Id", "Ad", seciliId);
        }

        public static double MesafeKm(double lat1, double lon1, double lat2, double lon2)
        {
            const double dunyaYaricapi = 6371;
            double dLat = (lat2 - lat1) * Math.PI / 180;
            double dLon = (lon2 - lon1) * Math.PI / 180;
            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                       Math.Cos(lat1 * Math.PI / 180) * Math.Cos(lat2 * Math.PI / 180) *
                       Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return dunyaYaricapi * c;
        }

        private static string CsvAlan(string? deger)
        {
            var temiz = (deger ?? "").Replace("\"", "\"\"").Replace("\r", " ").Replace("\n", " ");
            return $"\"{temiz}\"";
        }

        private void TeknisyenDurumunuGuncelle(Ariza ariza, int? eskiTeknisyenId, string eskiDurum)
        {
            if (eskiTeknisyenId != null && eskiTeknisyenId != ariza.TeknisyenId && eskiDurum == "Yolda")
            {
                var eski = _context.Teknisyenler.Find(eskiTeknisyenId);
                if (eski != null)
                {
                    eski.Musait = true;
                }
            }

            if (ariza.TeknisyenId == null)
            {
                return;
            }

            var teknisyen = _context.Teknisyenler.Find(ariza.TeknisyenId);
            if (teknisyen == null)
            {
                return;
            }

            if (ariza.Durum == "Yolda")
            {
                teknisyen.Musait = false;
            }
            else if (ariza.Durum == "Çözüldü")
            {
                teknisyen.Musait = true;

                if (ariza.Enlem != null && ariza.Boylam != null)
                {
                    teknisyen.Enlem = ariza.Enlem;
                    teknisyen.Boylam = ariza.Boylam;
                }
            }
            else if (eskiDurum == "Yolda")
            {
                teknisyen.Musait = true;
            }
        }

        private void GecmiseYaz(Ariza ariza, string eskiDurum, string yeniDurum, string? not = null)
        {
            ariza.DurumGecmisleri.Add(new DurumGecmisi
            {
                EskiDurum = eskiDurum,
                YeniDurum = yeniDurum,
                Degistiren = not == null ? Personel : $"{Personel} · {not}",
                Tarih = DateTime.Now
            });
        }

        public IActionResult Index(string? arama, string? durum, int sayfa = 1)
        {
            var tumKayitlar = _context.Arizalar.ToList();

            ViewBag.ToplamSayi = tumKayitlar.Count;
            ViewBag.BekleyenSayi = tumKayitlar.Count(a => a.Durum == "Bekliyor");
            ViewBag.YoldaSayi = tumKayitlar.Count(a => a.Durum == "Yolda");
            ViewBag.CozulenSayi = tumKayitlar.Count(a => a.Durum == "Çözüldü");
            ViewBag.AcilSayi = tumKayitlar.Count(a => a.Oncelik == "Acil");

            var sorgu = _context.Arizalar.Include(a => a.Teknisyen).AsQueryable();

            if (!string.IsNullOrEmpty(arama))
            {
                sorgu = sorgu.Where(a => a.MusteriAdi.Contains(arama));
            }

            if (!string.IsNullOrEmpty(durum))
            {
                sorgu = sorgu.Where(a => a.Durum == durum);
            }

            var filtreli = sorgu
                .OrderByDescending(a => a.Oncelik == "Acil")
                .ThenByDescending(a => a.BildirimTarihi);

            int filtreliSayi = filtreli.Count();
            int toplamSayfa = Math.Max(1, (int)Math.Ceiling(filtreliSayi / (double)SayfaBoyutu));
            sayfa = Math.Clamp(sayfa, 1, toplamSayfa);

            var liste = filtreli
                .Skip((sayfa - 1) * SayfaBoyutu)
                .Take(SayfaBoyutu)
                .ToList();

            ViewBag.Arama = arama;
            ViewBag.Durum = durum;
            ViewBag.Sayfa = sayfa;
            ViewBag.ToplamSayfa = toplamSayfa;
            ViewBag.FiltreliSayi = filtreliSayi;

            return View(liste);
        }

        public IActionResult Disari(string? arama, string? durum)
        {
            var sorgu = _context.Arizalar.Include(a => a.Teknisyen).AsQueryable();

            if (!string.IsNullOrEmpty(arama))
            {
                sorgu = sorgu.Where(a => a.MusteriAdi.Contains(arama));
            }

            if (!string.IsNullOrEmpty(durum))
            {
                sorgu = sorgu.Where(a => a.Durum == durum);
            }

            var liste = sorgu.OrderByDescending(a => a.BildirimTarihi).ToList();

            var sb = new StringBuilder();
            sb.AppendLine("Takip Kodu;Müşteri;Adres;Arıza Türü;Öncelik;Durum;Teknisyen;Bildirim Tarihi;Kapanış Tarihi;Çözüm Notu");

            foreach (var a in liste)
            {
                sb.AppendLine(string.Join(";", new[]
                {
                    CsvAlan(a.TakipKodu),
                    CsvAlan(a.MusteriAdi),
                    CsvAlan(a.Adres),
                    CsvAlan(a.ArizaTuru),
                    CsvAlan(a.Oncelik),
                    CsvAlan(a.Durum),
                    CsvAlan(a.Teknisyen?.AdSoyad),
                    CsvAlan(a.BildirimTarihi.ToString("dd.MM.yyyy HH:mm")),
                    CsvAlan(a.KapanisTarihi?.ToString("dd.MM.yyyy HH:mm")),
                    CsvAlan(a.CozumNotu)
                }));
            }

            var bom = Encoding.UTF8.GetPreamble();
            var icerik = bom.Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();

            return File(icerik, "text/csv", $"arizalar_{DateTime.Now:yyyyMMdd_HHmm}.csv");
        }

        public IActionResult Harita()
        {
            var arizalar = _context.Arizalar
                .Include(a => a.Teknisyen)
                .Where(a => a.Enlem != null && a.Boylam != null)
                .ToList()
                .Select(a => new
                {
                    id = a.Id,
                    musteri = a.MusteriAdi,
                    tur = a.ArizaTuru,
                    durum = a.Durum,
                    oncelik = a.Oncelik,
                    enlem = a.Enlem,
                    boylam = a.Boylam,
                    teknisyen = a.Teknisyen != null ? a.Teknisyen.AdSoyad : "Atanmadı"
                })
                .ToList();

            var teknisyenler = _context.Teknisyenler
                .Where(t => t.Enlem != null && t.Boylam != null)
                .Select(t => new { ad = t.AdSoyad, bolge = t.Bolge, musait = t.Musait, enlem = t.Enlem, boylam = t.Boylam })
                .ToList();

            ViewBag.Arizalar = JsonSerializer.Serialize(arizalar);
            ViewBag.TeknisyenKonumlari = JsonSerializer.Serialize(teknisyenler);

            return View();
        }

        public IActionResult Grafik()
        {
            var kayitlar = _context.Arizalar.ToList();
            int toplam = kayitlar.Count;
            int acil = kayitlar.Count(a => a.Oncelik == "Acil");
            int cozulen = kayitlar.Count(a => a.Durum == "Çözüldü");

            var aylik = kayitlar
                .GroupBy(a => a.BildirimTarihi.ToString("yyyy-MM"))
                .OrderBy(g => g.Key)
                .ToList();

            var turler = kayitlar
                .GroupBy(a => a.ArizaTuru)
                .OrderByDescending(g => g.Count())
                .Take(6)
                .ToList();

            var kapananlar = kayitlar.Where(a => a.KapanisTarihi != null).ToList();
            double ortalamaSaat = kapananlar.Any()
                ? kapananlar.Average(a => (a.KapanisTarihi!.Value - a.BildirimTarihi).TotalHours)
                : 0;

            ViewBag.ToplamSayi = toplam;
            ViewBag.AcilSayi = acil;
            ViewBag.AcilOrani = toplam == 0 ? 0 : acil * 100 / toplam;
            ViewBag.CozulmeOrani = toplam == 0 ? 0 : cozulen * 100 / toplam;
            ViewBag.OrtalamaSaat = Math.Round(ortalamaSaat, 1);
            ViewBag.KapananSayi = kapananlar.Count;

            ViewBag.AylarEtiket = JsonSerializer.Serialize(aylik.Select(g => g.Key));
            ViewBag.AylarVeri = JsonSerializer.Serialize(aylik.Select(g => g.Count()));
            ViewBag.TurEtiket = JsonSerializer.Serialize(turler.Select(g => g.Key));
            ViewBag.TurVeri = JsonSerializer.Serialize(turler.Select(g => g.Count()));
            ViewBag.DurumVeri = JsonSerializer.Serialize(new[]
            {
                kayitlar.Count(a => a.Durum == "Bekliyor"),
                kayitlar.Count(a => a.Durum == "Yolda"),
                kayitlar.Count(a => a.Durum == "Çözüldü")
            });

            return View();
        }

        public IActionResult Details(int id)
        {
            var ariza = _context.Arizalar
                .Include(a => a.Teknisyen)
                .Include(a => a.DurumGecmisleri)
                .FirstOrDefault(a => a.Id == id);

            if (ariza == null)
            {
                return NotFound();
            }

            if (ariza.Enlem != null && ariza.Boylam != null)
            {
                var mesafeler = _context.Teknisyenler
                    .Where(t => t.Enlem != null && t.Boylam != null)
                    .ToList()
                    .Select(t => new TeknisyenMesafe(
                        t.Id,
                        t.AdSoyad,
                        t.Bolge,
                        t.Musait,
                        t.Enlem!.Value,
                        t.Boylam!.Value,
                        MesafeKm(ariza.Enlem.Value, ariza.Boylam.Value, t.Enlem.Value, t.Boylam.Value)))
                    .OrderByDescending(x => x.Musait)
                    .ThenBy(x => x.Mesafe)
                    .ToList();

                ViewBag.Mesafeler = mesafeler;
            }

            return View(ariza);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult HizliDurum(int id, string yeniDurum, string? cozumNotu)
        {
            if (!GecerliDurumlar.Contains(yeniDurum))
            {
                return BadRequest();
            }

            var ariza = _context.Arizalar.Find(id);
            if (ariza == null)
            {
                return NotFound();
            }

            if (ariza.Durum == yeniDurum)
            {
                return RedirectToAction("Details", new { id });
            }

            if (yeniDurum == "Yolda" && ariza.TeknisyenId == null)
            {
                TempData["Hata"] = "Yola çıkmadan önce arızaya bir teknisyen ata.";
                return RedirectToAction("Details", new { id });
            }

            var eskiDurum = ariza.Durum;
            ariza.Durum = yeniDurum;

            if (yeniDurum == "Çözüldü")
            {
                ariza.KapanisTarihi ??= DateTime.Now;
                if (!string.IsNullOrWhiteSpace(cozumNotu))
                {
                    var temizNot = cozumNotu.Trim();
                    ariza.CozumNotu = temizNot.Length > 1000 ? temizNot[..1000] : temizNot;
                }
            }
            else
            {
                ariza.KapanisTarihi = null;
            }

            GecmiseYaz(ariza, eskiDurum, yeniDurum);
            TeknisyenDurumunuGuncelle(ariza, ariza.TeknisyenId, eskiDurum);

            _context.SaveChanges();
            TempData["Mesaj"] = $"Durum '{yeniDurum}' olarak güncellendi.";
            return RedirectToAction("Details", new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult TeknisyenAta(int id, int teknisyenId)
        {
            var ariza = _context.Arizalar.Find(id);
            var teknisyen = _context.Teknisyenler.Find(teknisyenId);

            if (ariza == null || teknisyen == null)
            {
                return NotFound();
            }

            if (ariza.TeknisyenId == teknisyenId)
            {
                return RedirectToAction("Details", new { id });
            }

            var eskiTeknisyenId = ariza.TeknisyenId;
            ariza.TeknisyenId = teknisyenId;

            GecmiseYaz(ariza, ariza.Durum, ariza.Durum, $"teknisyen atandı: {teknisyen.AdSoyad}");
            TeknisyenDurumunuGuncelle(ariza, eskiTeknisyenId, ariza.Durum);

            _context.SaveChanges();
            TempData["Mesaj"] = $"{teknisyen.AdSoyad} arızaya atandı.";
            return RedirectToAction("Details", new { id });
        }

        public IActionResult Create()
        {
            TeknisyenListesiHazirla();
            return View(new Ariza());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Ariza ariza)
        {
            if (!ModelState.IsValid)
            {
                TeknisyenListesiHazirla(ariza.TeknisyenId);
                return View(ariza);
            }

            ariza.BildirimTarihi = DateTime.Now;
            ariza.TakipKodu = Ariza.YeniTakipKodu();

            if (ariza.Durum == "Çözüldü")
            {
                ariza.KapanisTarihi = DateTime.Now;
            }

            GecmiseYaz(ariza, "-", ariza.Durum, "personel kaydı");

            _context.Arizalar.Add(ariza);
            TeknisyenDurumunuGuncelle(ariza, null, "-");
            _context.SaveChanges();
            return RedirectToAction("Index");
        }

        public IActionResult Edit(int id)
        {
            var ariza = _context.Arizalar.Find(id);
            if (ariza == null)
            {
                return NotFound();
            }

            TeknisyenListesiHazirla(ariza.TeknisyenId);
            return View(ariza);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(Ariza ariza)
        {
            if (!ModelState.IsValid)
            {
                TeknisyenListesiHazirla(ariza.TeknisyenId);
                return View(ariza);
            }

            var mevcut = _context.Arizalar.Find(ariza.Id);
            if (mevcut == null)
            {
                return NotFound();
            }

            var eskiDurum = mevcut.Durum;
            var eskiTeknisyenId = mevcut.TeknisyenId;

            mevcut.MusteriAdi = ariza.MusteriAdi;
            mevcut.Adres = ariza.Adres;
            mevcut.ArizaTuru = ariza.ArizaTuru;
            mevcut.Oncelik = ariza.Oncelik;
            mevcut.Durum = ariza.Durum;
            mevcut.TeknisyenId = ariza.TeknisyenId;
            mevcut.CozumNotu = ariza.CozumNotu;
            mevcut.Enlem = ariza.Enlem;
            mevcut.Boylam = ariza.Boylam;

            if (ariza.Durum == "Çözüldü")
            {
                mevcut.KapanisTarihi ??= DateTime.Now;
            }
            else
            {
                mevcut.KapanisTarihi = null;
            }

            if (eskiDurum != ariza.Durum)
            {
                GecmiseYaz(mevcut, eskiDurum, ariza.Durum);
            }

            TeknisyenDurumunuGuncelle(mevcut, eskiTeknisyenId, eskiDurum);

            _context.SaveChanges();
            TempData["Mesaj"] = "Kayıt güncellendi.";
            return RedirectToAction("Details", new { id = mevcut.Id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            var ariza = _context.Arizalar.Find(id);
            if (ariza != null)
            {
                if (ariza.Durum == "Yolda" && ariza.TeknisyenId != null)
                {
                    var teknisyen = _context.Teknisyenler.Find(ariza.TeknisyenId);
                    if (teknisyen != null)
                    {
                        teknisyen.Musait = true;
                    }
                }

                _context.Arizalar.Remove(ariza);
                _context.SaveChanges();
            }
            return RedirectToAction("Index");
        }
    }

    public record TeknisyenMesafe(int Id, string AdSoyad, string Bolge, bool Musait, double Enlem, double Boylam, double Mesafe);
}