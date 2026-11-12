using ArizaTakipMobil.Models;
using Microsoft.JSInterop;

namespace ArizaTakipMobil.Services;

// Ekranda kullanılan renk, ikon ve metin yardımcıları
public static class Gorunum
{
    public static string DurumRenk(string durum) => durum switch
    {
        "Bekliyor" => "#F59E0B",
        "Yolda" => "#2563EB",
        "Çözüldü" => "#16A34A",
        _ => "#6B7280"
    };

    public static string DurumSinif(string durum) => durum switch
    {
        "Bekliyor" => "bekliyor",
        "Yolda" => "yolda",
        "Çözüldü" => "cozuldu",
        _ => ""
    };

    public static string DurumIkon(string durum) => durum switch
    {
        "Bekliyor" => "⏳",
        "Yolda" => "🚐",
        "Çözüldü" => "✅",
        _ => "•"
    };

    public static string TurIkon(string tur) => tur switch
    {
        "Gaz kaçağı" => "🔥",
        "Sayaç arızası" => "📟",
        "İç tesisat" => "🔧",
        "Servis hattı" => "🛠️",
        "Gaz kesintisi" => "⛔",
        _ => "⚠️"
    };

    public static string GecenSure(DateTime tarih)
    {
        var fark = DateTime.Now - tarih;
        if (fark.TotalMinutes < 1) return "az önce";
        if (fark.TotalMinutes < 60) return $"{(int)fark.TotalMinutes} dk önce";
        if (fark.TotalHours < 24) return $"{(int)fark.TotalHours} sa önce";
        return $"{(int)fark.TotalDays} gün önce";
    }

    public static string SureYaz(double dakika) =>
        dakika < 60 ? $"{dakika:0} dk" : $"{dakika / 60:0.0} sa";

    // Şehir içi ortalama 30 km/sa + 5 dk hazırlık
    public static string TahminiVaris(double km) =>
        $"~{(int)Math.Ceiling(km / 30.0 * 60) + 5} dk";

    public static string Tarih(DateTime t) => t.ToString("dd.MM.yyyy HH:mm");
}

// Gerçek yol rotasının özeti (harita.js döner)
public record RotaOzet(double MesafeKm, double SureDk, bool Gercek);

// Haritaya gönderilen noktalar ve çizgiler (harita.js çizer)
public static class HaritaYardimci
{
    public static object ArizaNoktasi(Ariza a, bool detayLinki) => new
    {
        lat = a.Enlem,
        lng = a.Boylam,
        renk = Gorunum.DurumRenk(a.Durum),
        metin = Gorunum.TurIkon(a.ArizaTuru),
        baslik = $"{a.TakipKodu} · {a.ArizaTuru}",
        alt = $"{a.Durum} · {a.Oncelik}",
        link = detayLinki ? $"ariza/{a.Id}" : null,
        nabiz = a.Oncelik == "Acil" && a.Durum != "Çözüldü"
    };

    public static object EkipNoktasi(Teknisyen t) => new
    {
        lat = t.Enlem,
        lng = t.Boylam,
        renk = t.Musait ? "#16A34A" : "#6B7280",
        metin = "👷",
        baslik = t.AdSoyad,
        alt = $"{t.Uzmanlik} · {(t.Musait ? "Müsait" : "Görevde")}",
        link = (string?)null,
        nabiz = false
    };

    // Ekipten arızaya: gerçek yol rotası çizilir, durum "Yolda" ise ekip aracı yol boyunca hareket eder
    public static object Cizgi(Ariza a, Teknisyen t) => new
    {
        lat1 = a.Enlem,
        lng1 = a.Boylam,
        lat2 = t.Enlem,
        lng2 = t.Boylam,
        renk = Gorunum.DurumRenk(a.Durum),
        rota = true,
        hareket = a.Durum == "Yolda",
        ekipRenk = "#2563EB",
        ekipMetin = "🚐",
        ekipBaslik = t.AdSoyad,
        ekipAlt = $"{a.TakipKodu} adresine gidiyor"
    };

    public static ValueTask Ciz(IJSRuntime js, string elementId, List<object> noktalar, List<object>? cizgiler = null) =>
        js.InvokeVoidAsync("arizaHarita.genel", elementId, noktalar, cizgiler ?? new List<object>());

    public static ValueTask<RotaOzet[]> CizVeOlc(IJSRuntime js, string elementId, List<object> noktalar, List<object> cizgiler) =>
        js.InvokeAsync<RotaOzet[]>("arizaHarita.genel", elementId, noktalar, cizgiler);
}

// Ses, konfeti ve tema (efekt.js)
public static class Efekt
{
    public static async Task Ses(IJSRuntime js, AyarServisi ayar, string ad)
    {
        if (!ayar.SesAcik) return;
        try { await js.InvokeVoidAsync("efekt.ses", ad); } catch { }
    }

    public static async Task Konfeti(IJSRuntime js)
    {
        try { await js.InvokeVoidAsync("efekt.konfeti"); } catch { }
    }
}
