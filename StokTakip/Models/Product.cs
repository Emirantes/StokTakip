using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StokTakip.Models
{
    public class Product
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Ürün adı boş bırakılamaz")]
        public string? Name { get; set; }

        [Required(ErrorMessage = "Fiyat belirtilmelidir")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal? Price { get; set; }

        [Required(ErrorMessage = "Stok adedi girilmelidir")]
        public int? Stock { get; set; }

        [Required(ErrorMessage = "Bir kategori seçmelisiniz")]
        public int? KategoriId { get; set; }

        public virtual Kategori? Kategori { get; set; }

        // KULLANICI İZOLASYONU
        public int UserId { get; set; }

        public virtual User? User { get; set; }
    }
}