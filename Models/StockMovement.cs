using System.ComponentModel.DataAnnotations;

namespace StokTakip.Models
{
    public class StockMovement
    {
        public int Id { get; set; }

        public int ProductId { get; set; }

        public Product? Product { get; set; }

        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }

        [Required]
        [StringLength(20)]
        public string MovementType { get; set; } = string.Empty;

        public int CompanyId { get; set; }

        public Company? Company { get; set; }

        [StringLength(50)]
        public string? Username { get; set; }

        public int? UserId { get; set; }

        public User? User { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;
    }
}
