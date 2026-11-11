using CorumgazArizaTakip.Controllers;
using CorumgazArizaTakip.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CorumgazArizaTakip.Tests
{
    public class ArizaTests
    {
        private static ArizaDbContext YeniVeritabani()
        {
            var options = new DbContextOptionsBuilder<ArizaDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new ArizaDbContext(options);
        }

        private static ArizaController YeniController(ArizaDbContext db)
        {
            var http = new DefaultHttpContext();
            return new ArizaController(db)
            {
                ControllerContext = new ControllerContext { HttpContext = http },
                TempData = new TempDataDictionary(http, new BosTempDataSaglayici())
            };
        }

        private class BosTempDataSaglayici : ITempDataProvider
        {
            public IDictionary<string, object> LoadTempData(HttpContext context) => new Dictionary<string, object>();
            public void SaveTempData(HttpContext context, IDictionary<string, object> values) { }
        }

        private static Ariza OrnekAriza(string durum = "Bekliyor") => new Ariza
        {
            MusteriAdi = "Test",
            Adres = "Çorum",
            ArizaTuru = "Gaz",
            Oncelik = "Normal",
            Durum = durum
        };

        [Fact]
        public void TakipKodu_CRZ_ile_baslar_ve_10_karakterdir()
        {
            var kod = Ariza.YeniTakipKodu();

            Assert.StartsWith("CRZ-", kod);
            Assert.Equal(10, kod.Length);
        }

        [Fact]
        public void Ayni_takip_kodu_iki_kez_kaydedilemez()
        {
            using var db = YeniVeritabani();
            var a1 = OrnekAriza();
            var a2 = OrnekAriza();
            a1.TakipKodu = "CRZ-AAAAAA";
            a2.TakipKodu = "CRZ-AAAAAA";

            db.Arizalar.AddRange(a1, a2);
            db.SaveChanges();

            Assert.NotEqual(a1.TakipKodu, a2.TakipKodu);
        }

        [Fact]
        public void Create_yeni_kayitta_ilk_durum_gecmisi_olusur()
        {
            using var db = YeniVeritabani();
            var controller = YeniController(db);

            controller.Create(OrnekAriza());

            var kayit = db.Arizalar.Include(a => a.DurumGecmisleri).First();
            Assert.Single(kayit.DurumGecmisleri);
            Assert.Equal("Bekliyor", kayit.DurumGecmisleri.First().YeniDurum);
        }

        [Fact]
        public void Edit_durum_degisince_gecmise_yeni_kayit_eklenir()
        {
            using var db = YeniVeritabani();
            db.Arizalar.Add(OrnekAriza());
            db.SaveChanges();
            var kayitId = db.Arizalar.First().Id;

            var guncel = OrnekAriza("Çözüldü");
            guncel.Id = kayitId;
            YeniController(db).Edit(guncel);

            var kayit = db.Arizalar.Include(a => a.DurumGecmisleri).First();
            Assert.Equal("Çözüldü", kayit.Durum);
            Assert.NotNull(kayit.KapanisTarihi);
            Assert.Single(kayit.DurumGecmisleri);
            Assert.Equal("Çözüldü", kayit.DurumGecmisleri.First().YeniDurum);
        }

        [Fact]
        public void Yolda_olunca_teknisyen_gorevde_olur()
        {
            using var db = YeniVeritabani();
            var teknisyen = new Teknisyen { AdSoyad = "Usta", Musait = true, Enlem = 40.55, Boylam = 34.95 };
            db.Teknisyenler.Add(teknisyen);
            var ariza = OrnekAriza();
            ariza.TeknisyenId = teknisyen.Id;
            db.Arizalar.Add(ariza);
            db.SaveChanges();

            ariza.TeknisyenId = teknisyen.Id;
            db.SaveChanges();

            YeniController(db).HizliDurum(ariza.Id, "Yolda", null);

            Assert.False(db.Teknisyenler.Find(teknisyen.Id)!.Musait);
        }

        [Fact]
        public void Cozuldu_olunca_teknisyen_musait_olur_ve_konumu_tasinir()
        {
            using var db = YeniVeritabani();
            var teknisyen = new Teknisyen { AdSoyad = "Usta", Musait = false, Enlem = 40.55, Boylam = 34.95 };
            db.Teknisyenler.Add(teknisyen);
            db.SaveChanges();

            var ariza = OrnekAriza("Yolda");
            ariza.TeknisyenId = teknisyen.Id;
            ariza.Enlem = 40.56;
            ariza.Boylam = 34.97;
            db.Arizalar.Add(ariza);
            db.SaveChanges();

            YeniController(db).HizliDurum(ariza.Id, "Çözüldü", "Vana değiştirildi");

            var t = db.Teknisyenler.Find(teknisyen.Id)!;
            var a = db.Arizalar.Find(ariza.Id)!;
            Assert.True(t.Musait);
            Assert.Equal(40.56, t.Enlem);
            Assert.Equal(34.97, t.Boylam);
            Assert.Equal("Vana değiştirildi", a.CozumNotu);
            Assert.NotNull(a.KapanisTarihi);
        }

        [Fact]
        public void Mesafe_hesabi_yaklasik_dogrudur()
        {
            // Enlemde 0.009 derece yaklaşık 1 km eder.
            var km = ArizaController.MesafeKm(40.5500, 34.9500, 40.5590, 34.9500);

            Assert.InRange(km, 0.95, 1.05);
        }
    }
}