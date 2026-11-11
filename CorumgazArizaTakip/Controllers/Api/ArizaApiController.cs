using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CorumgazArizaTakip.Models;

namespace CorumgazArizaTakip.Controllers.Api
{
    [ApiController]
    [Route("api/arizalar")]
    [Authorize]
    public class ArizaApiController : ControllerBase
    {
        private const int MaksimumSayfaBoyutu = 100;
        private readonly ArizaDbContext _context;

        public ArizaApiController(ArizaDbContext context)
        {
            _context = context;
        }

        // GET api/arizalar?durum=Bekliyor&oncelik=Acil&sayfa=1&boyut=20
        [HttpGet]
        public IActionResult Liste(string? durum, string? oncelik, int sayfa = 1, int boyut = 20)
        {
            boyut = Math.Clamp(boyut, 1, MaksimumSayfaBoyutu);
            sayfa = Math.Max(1, sayfa);

            var sorgu = _context.Arizalar.Include(a => a.Teknisyen).AsQueryable();

            if (!string.IsNullOrEmpty(durum))
            {
                sorgu = sorgu.Where(a => a.Durum == durum);
            }

            if (!string.IsNullOrEmpty(oncelik))
            {
                sorgu = sorgu.Where(a => a.Oncelik == oncelik);
            }

            int toplam = sorgu.Count();

            var kayitlar = sorgu
                .OrderByDescending(a => a.BildirimTarihi)
                .Skip((sayfa - 1) * boyut)
                .Take(boyut)
                .ToList()
                .Select(ArizaDto.From)
                .ToList();

            return Ok(new
            {
                toplam,
                sayfa,
                boyut,
                toplamSayfa = (int)Math.Ceiling(toplam / (double)boyut),
                kayitlar
            });
        }

        // GET api/arizalar/5
        [HttpGet("{id:int}")]
        public IActionResult Detay(int id)
        {
            var ariza = _context.Arizalar
                .Include(a => a.Teknisyen)
                .FirstOrDefault(a => a.Id == id);

            if (ariza == null)
            {
                return NotFound(new { hata = "Arıza bulunamadı." });
            }

            return Ok(ArizaDto.From(ariza));
        }

        // GET api/arizalar/istatistik
        [HttpGet("istatistik")]
        public IActionResult Istatistik()
        {
            var kayitlar = _context.Arizalar.ToList();
            var kapananlar = kayitlar.Where(a => a.KapanisTarihi != null).ToList();

            return Ok(new
            {
                toplam = kayitlar.Count,
                bekliyor = kayitlar.Count(a => a.Durum == "Bekliyor"),
                yolda = kayitlar.Count(a => a.Durum == "Yolda"),
                cozuldu = kayitlar.Count(a => a.Durum == "Çözüldü"),
                acil = kayitlar.Count(a => a.Oncelik == "Acil"),
                musaitEkip = _context.Teknisyenler.Count(t => t.Musait),
                ortalamaCozumSaati = kapananlar.Any()
                    ? Math.Round(kapananlar.Average(a => (a.KapanisTarihi!.Value - a.BildirimTarihi).TotalHours), 1)
                    : 0
            });
        }

        // GET api/arizalar/sorgu/CRZ-ABC123  (herkese açık, kişisel bilgi döndürmez)
        [AllowAnonymous]
        [HttpGet("sorgu/{kod}")]
        public IActionResult Sorgu(string kod)
        {
            var temizKod = kod.Trim().ToUpper();
            var ariza = _context.Arizalar
                .Include(a => a.Teknisyen)
                .FirstOrDefault(a => a.TakipKodu == temizKod);

            if (ariza == null)
            {
                return NotFound(new { hata = "Bu takip koduyla kayıt bulunamadı." });
            }

            return Ok(new
            {
                takipKodu = ariza.TakipKodu,
                arizaTuru = ariza.ArizaTuru,
                oncelik = ariza.Oncelik,
                durum = ariza.Durum,
                ekip = ariza.Teknisyen?.AdSoyad,
                bildirimTarihi = ariza.BildirimTarihi,
                kapanisTarihi = ariza.KapanisTarihi
            });
        }
    }
}