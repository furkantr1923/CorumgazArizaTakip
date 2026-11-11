namespace CorumgazArizaTakip.Models
{
    public class DurumGecmisi
    {
        public int Id { get; set; }

        public int ArizaId { get; set; }
        public Ariza? Ariza { get; set; }

        public string EskiDurum { get; set; } = string.Empty;
        public string YeniDurum { get; set; } = string.Empty;

        public string? Degistiren { get; set; }

        public DateTime Tarih { get; set; } = DateTime.Now;
    }
}