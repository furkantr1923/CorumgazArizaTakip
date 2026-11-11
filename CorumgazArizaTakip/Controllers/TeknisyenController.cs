using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CorumgazArizaTakip.Models;

namespace CorumgazArizaTakip.Controllers
{
    [Authorize]
    public class TeknisyenController : Controller
    {
        private readonly ArizaDbContext _context;

        public TeknisyenController(ArizaDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var arizalar = _context.Arizalar
                .Where(a => a.TeknisyenId != null)
                .Select(a => new { a.TeknisyenId, a.Durum })
                .ToList();

            var liste = _context.Teknisyenler
                .OrderBy(t => t.AdSoyad)
                .ToList()
                .Select(t => new TeknisyenSatir(
                    t,
                    arizalar.Count(a => a.TeknisyenId == t.Id && a.Durum != "Çözüldü"),
                    arizalar.Count(a => a.TeknisyenId == t.Id && a.Durum == "Çözüldü")))
                .ToList();

            return View(liste);
        }

        public IActionResult Create()
        {
            ViewBag.Baslik = "Yeni Ekip Üyesi";
            return View("Form", new Teknisyen());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create([Bind("AdSoyad,Uzmanlik,Bolge,Musait,Enlem,Boylam")] Teknisyen teknisyen)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Baslik = "Yeni Ekip Üyesi";
                return View("Form", teknisyen);
            }

            _context.Teknisyenler.Add(teknisyen);
            _context.SaveChanges();
            TempData["Mesaj"] = $"{teknisyen.AdSoyad} eklendi.";
            return RedirectToAction("Index");
        }

        public IActionResult Edit(int id)
        {
            var teknisyen = _context.Teknisyenler.Find(id);
            if (teknisyen == null)
            {
                return NotFound();
            }

            ViewBag.Baslik = "Ekip Üyesini Düzenle";
            return View("Form", teknisyen);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit([Bind("Id,AdSoyad,Uzmanlik,Bolge,Musait,Enlem,Boylam")] Teknisyen teknisyen)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Baslik = "Ekip Üyesini Düzenle";
                return View("Form", teknisyen);
            }

            var mevcut = _context.Teknisyenler.Find(teknisyen.Id);
            if (mevcut == null)
            {
                return NotFound();
            }

            mevcut.AdSoyad = teknisyen.AdSoyad;
            mevcut.Uzmanlik = teknisyen.Uzmanlik;
            mevcut.Bolge = teknisyen.Bolge;
            mevcut.Musait = teknisyen.Musait;
            mevcut.Enlem = teknisyen.Enlem;
            mevcut.Boylam = teknisyen.Boylam;

            _context.SaveChanges();
            TempData["Mesaj"] = $"{mevcut.AdSoyad} güncellendi.";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DurumDegistir(int id)
        {
            var teknisyen = _context.Teknisyenler.Find(id);
            if (teknisyen != null)
            {
                teknisyen.Musait = !teknisyen.Musait;
                _context.SaveChanges();
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            var teknisyen = _context.Teknisyenler.Find(id);
            if (teknisyen != null)
            {
                var bagliArizalar = _context.Arizalar.Where(a => a.TeknisyenId == id).ToList();
                foreach (var a in bagliArizalar)
                {
                    a.TeknisyenId = null;
                }

                _context.Teknisyenler.Remove(teknisyen);
                _context.SaveChanges();
                TempData["Mesaj"] = $"{teknisyen.AdSoyad} silindi. Atandığı arızalar 'Atanmadı' durumuna alındı.";
            }
            return RedirectToAction("Index");
        }
    }

    public record TeknisyenSatir(Teknisyen Teknisyen, int AcikIs, int Cozulen);
}