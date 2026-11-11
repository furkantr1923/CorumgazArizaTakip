using System.ComponentModel.DataAnnotations;

namespace CorumgazArizaTakip.Models
{
    public class Ariza
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Müşteri adı boş bırakılamaz.")]
        [StringLength(100, ErrorMessage = "Müşteri adı en fazla 100 karakter olabilir.")]
        public string MusteriAdi { get; set; } = string.Empty;

        [Required(ErrorMessage = "Adres boş bırakılamaz.")]
        [StringLength(200, ErrorMessage = "Adres en fazla 200 karakter olabilir.")]
        public string Adres { get; set; } = string.Empty;

        [Required(ErrorMessage = "Arıza türü boş bırakılamaz.")]
        [StringLength(100, ErrorMessage = "Arıza türü en fazla 100 karakter olabilir.")]
        public string ArizaTuru { get; set; } = string.Empty;

        [Required]
        public string Oncelik { get; set; } = "Normal";

        [Required]
        public string Durum { get; set; } = "Bekliyor";

        public DateTime BildirimTarihi { get; set; } = DateTime.Now;

        public string TakipKodu { get; set; } = string.Empty;

        [StringLength(1000, ErrorMessage = "Çözüm notu en fazla 1000 karakter olabilir.")]
        public string? CozumNotu { get; set; }

        public DateTime? KapanisTarihi { get; set; }

        public double? Enlem { get; set; }

        public double? Boylam { get; set; }

        public DateTime? KvkkOnayTarihi { get; set; }

        public int? TeknisyenId { get; set; }
        public Teknisyen? Teknisyen { get; set; }

        public ICollection<DurumGecmisi> DurumGecmisleri { get; set; } = new List<DurumGecmisi>();

        public static string YeniTakipKodu()
        {
            const string karakterler = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            var kod = new string(Enumerable.Range(0, 6)
                .Select(_ => karakterler[Random.Shared.Next(karakterler.Length)])
                .ToArray());
            return "CRZ-" + kod;
        }
    }
}