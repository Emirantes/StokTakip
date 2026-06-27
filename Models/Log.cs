using System.ComponentModel.DataAnnotations;

namespace StokTakip.Models
{
    public class Log
    {
        public int Id { get; set; }

        [Required]
        [StringLength(500)]
        public string Islem { get; set; } = string.Empty;

        public DateTime Tarih { get; set; } = DateTime.Now;

        [StringLength(50)]
        public string? KullaniciAdi { get; set; }

        [StringLength(20)]
        public string? Rol { get; set; }

        [StringLength(100)]
        public string? IpAddress { get; set; }

        public int CompanyId { get; set; }

        public Company? Company { get; set; }

        public int? UserId { get; set; }

        public User? User { get; set; }
    }
}
