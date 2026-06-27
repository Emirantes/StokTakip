using System.ComponentModel.DataAnnotations;

namespace StokTakip.Models
{
    public class Kategori
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Kategori adı zorunludur.")]
        [StringLength(100, MinimumLength = 2)]
        public string KategoriAd { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Aciklama { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public bool IsActive { get; set; } = true;

        public int CompanyId { get; set; }

        public Company? Company { get; set; }

        public virtual ICollection<Product> Products { get; set; } =
            new List<Product>();
    }
}