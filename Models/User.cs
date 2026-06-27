using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace StokTakip.Models
{
    public class User : IdentityUser<int>
    {
        [Required]
        [StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        [NotMapped]
        [Required]
        [StringLength(50)]
        public string Username
        {
            get => UserName ?? string.Empty;
            set => UserName = value;
        }

        [NotMapped]
        [MinLength(8)]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [StringLength(20)]
        public string Role { get; set; } = UserRole.Personel.ToString();

        public bool IsActive { get; set; } = true;

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public int CompanyId { get; set; }

        public Company? Company { get; set; }

        public bool CanManageUsers =>
            Role == UserRole.Owner.ToString();

        public virtual ICollection<Product> Products { get; set; } =
            new List<Product>();
    }
}
