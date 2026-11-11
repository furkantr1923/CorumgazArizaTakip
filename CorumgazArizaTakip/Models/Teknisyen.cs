using System.ComponentModel.DataAnnotations;

namespace CorumgazArizaTakip.Models
{
    public class Teknisyen
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Ad soyad boş bırakılamaz.")]
        public string AdSoyad { get; set; } = string.Empty;

        public string Uzmanlik { get; set; } = "Genel";

        public string Bolge { get; set; } = "Merkez";

        public bool Musait { get; set; } = true;

        public double? Enlem { get; set; }
        public double? Boylam { get; set; }

        public ICollection<Ariza> Arizalar { get; set; } = new List<Ariza>();
    }
}