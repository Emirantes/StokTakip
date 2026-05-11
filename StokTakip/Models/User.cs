using System.ComponentModel.DataAnnotations;

namespace StokTakip.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required]
        public required string Username { get; set; }

        [Required]
        [EmailAddress]
        public required string Email { get; set; }

        [Required]
        public required string Password { get; set; }

        [Required]
        public required string FullName { get; set; }

        // KULLANICIYA AİT ÜRÜNLER
        public virtual ICollection<Product>? Products { get; set; }
    }
}