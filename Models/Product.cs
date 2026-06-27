using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StokTakip.Models
{
    public class Product
    {
        public int Id { get; set; }

        [Required]
        [StringLength(150)]
        public string Name { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        [Range(0, 999999999)]
        public decimal? Price { get; set; }

        [Range(0, int.MaxValue)]
        public int? Stock { get; set; }

        public int? KategoriId { get; set; }

        public virtual Kategori? Kategori { get; set; }

        public int CompanyId { get; set; }

        public Company? Company { get; set; }

        public int UserId { get; set; }

        public User? User { get; set; }

        public int? CreatedByUserId { get; set; }

        public User? CreatedByUser { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public bool IsActive { get; set; } = true;

        [Range(0, int.MaxValue)]
        public int CriticalStock { get; set; } = 10;

        [StringLength(50)]
        public string StockType { get; set; } = "Personal";

        [NotMapped]
        public decimal TotalValue => (Price ?? 0) * (Stock ?? 0);
    }
}
