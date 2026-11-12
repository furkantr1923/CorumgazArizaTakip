namespace ArizaTakipMobil.Services;

// Personel girişi. Uygulama tamamen telefonda çalıştığı için demo hesabı kullanılır.
public class OturumServisi
{
    public const string DemoKullanici = "admin";
    public const string DemoSifre = "demo123";

    public string? KullaniciAdi { get; private set; }
    public bool GirisYapti => KullaniciAdi != null;

    public event Action? Degisti;

    public bool Giris(string kullanici, string sifre)
    {
        if (kullanici.Trim().Equals(DemoKullanici, StringComparison.OrdinalIgnoreCase) && sifre == DemoSifre)
        {
            KullaniciAdi = DemoKullanici;
            Degisti?.Invoke();
            return true;
        }
        return false;
    }

    // Parmak izi doğrulanınca demo hesabıyla giriş
    public void BiyometrikGiris()
    {
        KullaniciAdi = DemoKullanici;
        Degisti?.Invoke();
    }

    // Panelde uyarısı gösterilmiş acil arızalar (aynı alarm tekrar çalmasın)
    public HashSet<int> GorulenAciller { get; } = new();

    public void Cikis()
    {
        KullaniciAdi = null;
        Degisti?.Invoke();
    }
}
