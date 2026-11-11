namespace CorumgazArizaTakip.Models
{
    public class Kullanici
    {
        public int Id { get; set; }
        public string KullaniciAdi { get; set; } = string.Empty;
        public string SifreHash { get; set; } = string.Empty;
    }
}