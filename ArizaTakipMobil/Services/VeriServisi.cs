using ArizaTakipMobil.Models;
using Microsoft.Maui.Storage;
using SQLite;

namespace ArizaTakipMobil.Services;

public record Ozet(int Toplam, int Bekliyor, int Yolda, int Cozuldu, int MusaitEkip, int EkipSayisi, double? OrtalamaCozumDakika, double? OrtalamaPuan);

// Tüm veri işlemleri burada: veritabanı telefonun içinde (arizalar.db3)
public class VeriServisi
{
    public static readonly string[] ArizaTurleri = { "Gaz kaçağı", "Sayaç arızası", "İç tesisat", "Servis hattı", "Gaz kesintisi" };
    public static readonly string[] Durumlar = { "Bekliyor", "Yolda", "Çözüldü" };

    private const string KodHarfleri = "ABCDEFGHJKLMNPRSTUVYZ23456789";

    private SQLiteAsyncConnection? _db;
    private readonly SemaphoreSlim _kilit = new(1, 1);

    // Veritabanını ilk kullanımda açar, tabloları ve örnek verileri oluşturur
    private async Task<SQLiteAsyncConnection> Db()
    {
        if (_db != null) return _db;

        await _kilit.WaitAsync();
        try
        {
            if (_db == null)
            {
                var yol = System.IO.Path.Combine(FileSystem.AppDataDirectory, "arizalar.db3");
                var baglanti = new SQLiteAsyncConnection(yol);
                await baglanti.CreateTableAsync<Teknisyen>();
                await baglanti.CreateTableAsync<Ariza>();
                await baglanti.CreateTableAsync<DurumGecmisi>();
                await OrnekVerileriEkle(baglanti);
                _db = baglanti;
            }
            return _db;
        }
        finally
        {
            _kilit.Release();
        }
    }

    // ---------- Okuma ----------

    public async Task<List<Ariza>> Arizalar()
    {
        var db = await Db();
        var liste = await db.Table<Ariza>().ToListAsync();
        return liste.OrderByDescending(a => a.BildirimTarihi).ToList();
    }

    public async Task<Ariza?> ArizaGetir(int id)
    {
        var db = await Db();
        return await db.FindAsync<Ariza>(id);
    }

    public async Task<Ariza?> KodIleBul(string? kod)
    {
        kod = (kod ?? "").Trim().ToUpperInvariant().Replace(" ", "");
        if (kod.Length == 0) return null;
        if (!kod.StartsWith("CRZ-"))
            kod = "CRZ-" + kod.Replace("CRZ", "").TrimStart('-');

        var db = await Db();
        return await db.Table<Ariza>().Where(a => a.TakipKodu == kod).FirstOrDefaultAsync();
    }

    public async Task<List<DurumGecmisi>> Gecmis(int arizaId)
    {
        var db = await Db();
        var liste = await db.Table<DurumGecmisi>().Where(g => g.ArizaId == arizaId).ToListAsync();
        return liste.OrderBy(g => g.Tarih).ToList();
    }

    public async Task<List<Teknisyen>> Teknisyenler()
    {
        var db = await Db();
        var liste = await db.Table<Teknisyen>().ToListAsync();
        return liste.OrderBy(t => t.AdSoyad).ToList();
    }

    public async Task<Teknisyen?> TeknisyenGetir(int? id)
    {
        if (id == null) return null;
        var db = await Db();
        return await db.FindAsync<Teknisyen>(id.Value);
    }

    public async Task<Ozet> OzetGetir()
    {
        var arizalar = await Arizalar();
        var ekipler = await Teknisyenler();
        var cozulen = arizalar.Where(a => a.Durum == "Çözüldü" && a.KapanisTarihi != null).ToList();
        double? ortalama = cozulen.Count == 0
            ? null
            : cozulen.Average(a => (a.KapanisTarihi!.Value - a.BildirimTarihi).TotalMinutes);
        var puanlilar = arizalar.Where(a => a.Puan != null).ToList();
        double? ortalamaPuan = puanlilar.Count == 0 ? null : puanlilar.Average(a => a.Puan!.Value);

        return new Ozet(
            arizalar.Count,
            arizalar.Count(a => a.Durum == "Bekliyor"),
            arizalar.Count(a => a.Durum == "Yolda"),
            arizalar.Count(a => a.Durum == "Çözüldü"),
            ekipler.Count(t => t.Musait),
            ekipler.Count,
            ortalama,
            ortalamaPuan);
    }

    // ---------- Yazma ----------

    // Müşteri bildirimi: takip kodu üretir, en uygun ekibi otomatik atar
    public async Task<Ariza> BildirimEkle(Ariza ariza)
    {
        var db = await Db();
        var ekipler = await db.Table<Teknisyen>().ToListAsync();
        var (ekip, _) = EnUygunEkip(ekipler, ariza.Enlem, ariza.Boylam, ariza.ArizaTuru);

        ariza.TakipKodu = await YeniTakipKodu(db);
        ariza.Durum = "Bekliyor";
        ariza.BildirimTarihi = DateTime.Now;
        ariza.KvkkOnayTarihi = DateTime.Now;
        ariza.TeknisyenId = ekip?.Id;
        await db.InsertAsync(ariza);

        await db.InsertAsync(new DurumGecmisi
        {
            ArizaId = ariza.Id,
            Durum = "Bekliyor",
            Tarih = DateTime.Now,
            Degistiren = "Müşteri (mobil)",
            Aciklama = ekip == null ? "Kayıt oluşturuldu" : $"Otomatik atama: {ekip.AdSoyad}"
        });

        return ariza;
    }

    public async Task DurumDegistir(int arizaId, string yeniDurum, string? cozumNotu, string degistiren)
    {
        var db = await Db();
        var ariza = await db.FindAsync<Ariza>(arizaId);
        if (ariza == null || ariza.Durum == yeniDurum) return;

        ariza.Durum = yeniDurum;
        if (yeniDurum == "Çözüldü")
        {
            ariza.KapanisTarihi = DateTime.Now;
            ariza.CozumNotu = cozumNotu?.Trim();
        }
        else
        {
            ariza.KapanisTarihi = null;
        }
        await db.UpdateAsync(ariza);

        await db.InsertAsync(new DurumGecmisi
        {
            ArizaId = ariza.Id,
            Durum = yeniDurum,
            Tarih = DateTime.Now,
            Degistiren = degistiren,
            Aciklama = yeniDurum == "Çözüldü" ? ariza.CozumNotu : null
        });

        // Ekip durumu: yoldaysa görevde, çözülünce müsait ve arıza noktasına taşınır
        if (ariza.TeknisyenId is int tid)
        {
            var ekip = await db.FindAsync<Teknisyen>(tid);
            if (ekip != null)
            {
                if (yeniDurum == "Yolda")
                {
                    ekip.Musait = false;
                }
                else if (yeniDurum == "Çözüldü")
                {
                    ekip.Musait = true;
                    ekip.Enlem = ariza.Enlem;
                    ekip.Boylam = ariza.Boylam;
                }
                await db.UpdateAsync(ekip);
            }
        }
    }

    public async Task TeknisyenAta(int arizaId, int? teknisyenId, string degistiren)
    {
        var db = await Db();
        var ariza = await db.FindAsync<Ariza>(arizaId);
        if (ariza == null || ariza.TeknisyenId == teknisyenId) return;

        // Eski ekip yoldaysa serbest bırakılır
        if (ariza.TeknisyenId is int eskiId && ariza.Durum == "Yolda")
        {
            var eski = await db.FindAsync<Teknisyen>(eskiId);
            if (eski != null)
            {
                eski.Musait = true;
                await db.UpdateAsync(eski);
            }
        }

        ariza.TeknisyenId = teknisyenId;
        await db.UpdateAsync(ariza);

        Teknisyen? yeni = null;
        if (teknisyenId is int yeniId)
        {
            yeni = await db.FindAsync<Teknisyen>(yeniId);
            if (yeni != null && ariza.Durum == "Yolda")
            {
                yeni.Musait = false;
                await db.UpdateAsync(yeni);
            }
        }

        await db.InsertAsync(new DurumGecmisi
        {
            ArizaId = ariza.Id,
            Durum = ariza.Durum,
            Tarih = DateTime.Now,
            Degistiren = degistiren,
            Aciklama = yeni == null ? "Ekip ataması kaldırıldı" : $"Ekip atandı: {yeni.AdSoyad}"
        });
    }

    // Müşteri, çözülen arızaya 1-5 yıldız verir
    public async Task Degerlendir(int arizaId, int puan, string? yorum)
    {
        var db = await Db();
        var ariza = await db.FindAsync<Ariza>(arizaId);
        if (ariza == null || ariza.Durum != "Çözüldü") return;

        ariza.Puan = Math.Clamp(puan, 1, 5);
        ariza.Yorum = string.IsNullOrWhiteSpace(yorum) ? null : yorum.Trim();
        ariza.DegerlendirmeTarihi = DateTime.Now;
        await db.UpdateAsync(ariza);
    }

    public async Task MusaitlikDegistir(int teknisyenId)
    {
        var db = await Db();
        var ekip = await db.FindAsync<Teknisyen>(teknisyenId);
        if (ekip == null) return;
        ekip.Musait = !ekip.Musait;
        await db.UpdateAsync(ekip);
    }

    // ---------- Yardımcılar ----------

    // Önce müsait ekipler; uzman ekip en yakına göre en fazla 3 km uzaksa uzman seçilir
    public static (Teknisyen? Ekip, double Mesafe) EnUygunEkip(IEnumerable<Teknisyen> ekipler, double enlem, double boylam, string tur)
    {
        var adaylar = ekipler
            .Select(t => (Ekip: t, Mesafe: MesafeKm(enlem, boylam, t.Enlem, t.Boylam)))
            .ToList();
        if (adaylar.Count == 0) return (null, 0);

        var musaitler = adaylar.Where(x => x.Ekip.Musait).ToList();
        var havuz = musaitler.Count > 0 ? musaitler : adaylar;

        var enYakin = havuz.OrderBy(x => x.Mesafe).First();
        var uzmanlar = havuz.Where(x => UzmanMi(x.Ekip.Uzmanlik, tur)).OrderBy(x => x.Mesafe).ToList();

        if (uzmanlar.Count > 0 && uzmanlar[0].Mesafe <= enYakin.Mesafe + 3)
            return (uzmanlar[0].Ekip, uzmanlar[0].Mesafe);

        return (enYakin.Ekip, enYakin.Mesafe);
    }

    private static bool UzmanMi(string uzmanlik, string tur) =>
        uzmanlik == tur
        || (tur == "Gaz kaçağı" && uzmanlik == "Acil müdahale")
        || (tur == "Gaz kesintisi" && uzmanlik == "Servis hattı");

    // Haversine: iki koordinat arası kuş uçuşu mesafe (km)
    public static double MesafeKm(double enlem1, double boylam1, double enlem2, double boylam2)
    {
        const double R = 6371;
        double Rad(double derece) => derece * Math.PI / 180;
        var dEnlem = Rad(enlem2 - enlem1);
        var dBoylam = Rad(boylam2 - boylam1);
        var a = Math.Sin(dEnlem / 2) * Math.Sin(dEnlem / 2)
              + Math.Cos(Rad(enlem1)) * Math.Cos(Rad(enlem2)) * Math.Sin(dBoylam / 2) * Math.Sin(dBoylam / 2);
        return R * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }

    private static async Task<string> YeniTakipKodu(SQLiteAsyncConnection db)
    {
        while (true)
        {
            var kod = "CRZ-" + new string(Enumerable.Range(0, 6)
                .Select(_ => KodHarfleri[Random.Shared.Next(KodHarfleri.Length)])
                .ToArray());
            var adet = await db.Table<Ariza>().Where(a => a.TakipKodu == kod).CountAsync();
            if (adet == 0) return kod;
        }
    }

    // İlk açılışta 10 ekip ve 3 örnek arıza eklenir (uygulama boş görünmesin)
    private static async Task OrnekVerileriEkle(SQLiteAsyncConnection db)
    {
        if (await db.Table<Teknisyen>().CountAsync() > 0) return;

        var ekipler = new List<Teknisyen>
        {
            new() { AdSoyad = "Ahmet Yılmaz", Uzmanlik = "Gaz kaçağı", Bolge = "Merkez", Enlem = 40.5506, Boylam = 34.9556, Musait = false },
            new() { AdSoyad = "Mehmet Kaya", Uzmanlik = "Sayaç arızası", Bolge = "Kuzey", Enlem = 40.5700, Boylam = 34.9550 },
            new() { AdSoyad = "Ayşe Demir", Uzmanlik = "İç tesisat", Bolge = "Güney", Enlem = 40.5330, Boylam = 34.9560 },
            new() { AdSoyad = "Fatma Çelik", Uzmanlik = "Acil müdahale", Bolge = "Doğu", Enlem = 40.5510, Boylam = 34.9800 },
            new() { AdSoyad = "Emre Şahin", Uzmanlik = "Gaz kaçağı", Bolge = "Batı", Enlem = 40.5500, Boylam = 34.9300 },
            new() { AdSoyad = "Zeynep Arslan", Uzmanlik = "Sayaç arızası", Bolge = "Kuzeydoğu", Enlem = 40.5650, Boylam = 34.9750 },
            new() { AdSoyad = "Murat Öztürk", Uzmanlik = "Servis hattı", Bolge = "Kuzeybatı", Enlem = 40.5650, Boylam = 34.9350 },
            new() { AdSoyad = "Elif Koç", Uzmanlik = "İç tesisat", Bolge = "Güneydoğu", Enlem = 40.5370, Boylam = 34.9750 },
            new() { AdSoyad = "Burak Aydın", Uzmanlik = "Acil müdahale", Bolge = "Güneybatı", Enlem = 40.5370, Boylam = 34.9380 },
            new() { AdSoyad = "Selin Yıldız", Uzmanlik = "Servis hattı", Bolge = "Sanayi", Enlem = 40.5450, Boylam = 34.9950 }
        };
        await db.InsertAllAsync(ekipler);

        var simdi = DateTime.Now;

        async Task Ekle(Ariza a, params (string Durum, DateTime Tarih, string? Not)[] adimlar)
        {
            await db.InsertAsync(a);
            foreach (var adim in adimlar)
            {
                await db.InsertAsync(new DurumGecmisi
                {
                    ArizaId = a.Id,
                    Durum = adim.Durum,
                    Tarih = adim.Tarih,
                    Degistiren = adim.Durum == "Bekliyor" ? "Müşteri (mobil)" : "admin",
                    Aciklama = adim.Not
                });
            }
        }

        await Ekle(new Ariza
        {
            TakipKodu = "CRZ-DEMO01", AdSoyad = "Ali Demir", Telefon = "0555 000 00 01",
            Adres = "Gülabibey Mah. Örnek Sok. No:5, Merkez", ArizaTuru = "Gaz kaçağı", Oncelik = "Acil",
            Aciklama = "Mutfakta gaz kokusu var, vanayı kapattık.", Durum = "Yolda",
            Enlem = 40.5480, Boylam = 34.9520, TeknisyenId = ekipler[0].Id,
            BildirimTarihi = simdi.AddMinutes(-25), KvkkOnayTarihi = simdi.AddMinutes(-25)
        },
        ("Bekliyor", simdi.AddMinutes(-25), $"Otomatik atama: {ekipler[0].AdSoyad}"),
        ("Yolda", simdi.AddMinutes(-15), null));

        await Ekle(new Ariza
        {
            TakipKodu = "CRZ-DEMO02", AdSoyad = "Merve Kara", Telefon = "0555 000 00 02",
            Adres = "Ulukavak Mah. Deneme Cad. No:12, Merkez", ArizaTuru = "Sayaç arızası", Oncelik = "Normal",
            Aciklama = "Sayaç ekranı yanmıyor.", Durum = "Bekliyor",
            Enlem = 40.5675, Boylam = 34.9610, TeknisyenId = ekipler[1].Id,
            BildirimTarihi = simdi.AddMinutes(-10), KvkkOnayTarihi = simdi.AddMinutes(-10)
        },
        ("Bekliyor", simdi.AddMinutes(-10), $"Otomatik atama: {ekipler[1].AdSoyad}"));

        var dun = simdi.AddDays(-1);
        await Ekle(new Ariza
        {
            TakipKodu = "CRZ-DEMO03", AdSoyad = "Kemal Usta", Telefon = "0555 000 00 03",
            Adres = "Buharaevler Mah. Örnek Sok. No:8, Merkez", ArizaTuru = "İç tesisat", Oncelik = "Normal",
            Aciklama = "Kombi bağlantısında sızıntı şüphesi.", Durum = "Çözüldü",
            Enlem = 40.5350, Boylam = 34.9530, TeknisyenId = ekipler[2].Id,
            BildirimTarihi = dun, KvkkOnayTarihi = dun, KapanisTarihi = dun.AddMinutes(95),
            CozumNotu = "Kombi bağlantısındaki conta değiştirildi, sızdırmazlık testi yapıldı.",
            Puan = 5, Yorum = "Ekip çok hızlı geldi, teşekkürler!", DegerlendirmeTarihi = dun.AddMinutes(120)
        },
        ("Bekliyor", dun, $"Otomatik atama: {ekipler[2].AdSoyad}"),
        ("Yolda", dun.AddMinutes(20), null),
        ("Çözüldü", dun.AddMinutes(95), "Kombi bağlantısındaki conta değiştirildi, sızdırmazlık testi yapıldı."));
    }
}
