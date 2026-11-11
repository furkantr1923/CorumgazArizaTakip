namespace CorumgazArizaTakip.Models
{
    public class ArizaDto
    {
        public int Id { get; set; }
        public string TakipKodu { get; set; } = string.Empty;
        public string MusteriAdi { get; set; } = string.Empty;
        public string Adres { get; set; } = string.Empty;
        public string ArizaTuru { get; set; } = string.Empty;
        public string Oncelik { get; set; } = string.Empty;
        public string Durum { get; set; } = string.Empty;
        public string? TeknisyenAdi { get; set; }
        public DateTime BildirimTarihi { get; set; }
        public DateTime? KapanisTarihi { get; set; }
        public string? CozumNotu { get; set; }
        public double? Enlem { get; set; }
        public double? Boylam { get; set; }

        public static ArizaDto From(Ariza a)
        {
            return new ArizaDto
            {
                Id = a.Id,
                TakipKodu = a.TakipKodu,
                MusteriAdi = a.MusteriAdi,
                Adres = a.Adres,
                ArizaTuru = a.ArizaTuru,
                Oncelik = a.Oncelik,
                Durum = a.Durum,
                TeknisyenAdi = a.Teknisyen?.AdSoyad,
                BildirimTarihi = a.BildirimTarihi,
                KapanisTarihi = a.KapanisTarihi,
                CozumNotu = a.CozumNotu,
                Enlem = a.Enlem,
                Boylam = a.Boylam
            };
        }
    }
}