using SQLite;

namespace ArizaTakipMobil.Models;

// Telefondaki SQLite veritabanında tutulan tablolar

public class Ariza
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Indexed(Unique = true)]
    public string TakipKodu { get; set; } = "";

    public string AdSoyad { get; set; } = "";
    public string Telefon { get; set; } = "";
    public string Adres { get; set; } = "";
    public string ArizaTuru { get; set; } = "";
    public string Oncelik { get; set; } = "Normal";   // Normal / Acil
    public string Aciklama { get; set; } = "";
    public string Durum { get; set; } = "Bekliyor";   // Bekliyor / Yolda / Çözüldü

    public double Enlem { get; set; }
    public double Boylam { get; set; }

    public int? TeknisyenId { get; set; }

    public DateTime BildirimTarihi { get; set; } = DateTime.Now;
    public DateTime? KapanisTarihi { get; set; }
    public string? CozumNotu { get; set; }
    public DateTime KvkkOnayTarihi { get; set; }

    // Yeni: fotoğraf ve müşteri değerlendirmesi
    public string? FotoYolu { get; set; }
    public int? Puan { get; set; }              // 1-5 yıldız
    public string? Yorum { get; set; }
    public DateTime? DegerlendirmeTarihi { get; set; }
}

public class Teknisyen
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public string AdSoyad { get; set; } = "";
    public string Uzmanlik { get; set; } = "";
    public string Bolge { get; set; } = "";
    public bool Musait { get; set; } = true;

    public double Enlem { get; set; }
    public double Boylam { get; set; }
}

public class DurumGecmisi
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Indexed]
    public int ArizaId { get; set; }

    public string Durum { get; set; } = "";
    public DateTime Tarih { get; set; } = DateTime.Now;
    public string Degistiren { get; set; } = "";
    public string? Aciklama { get; set; }
}
