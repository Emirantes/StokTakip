using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StokTakip.Models
{
    public class Company
    {
        public int Id { get; set; }

        [Required]
        [StringLength(150)]
        public string CompanyName { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string CompanyCode { get; set; } = string.Empty;

        [StringLength(20)]
        public string PlanType { get; set; } =
            StokTakip.Models.PlanType.Team.ToString();

        [NotMapped]
        public string SystemType
        {
            get => PlanType;
            set => PlanType = value;
        }

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public virtual ICollection<User> Users { get; set; } =
            new List<User>();

        public virtual ICollection<Product> Products { get; set; } =
            new List<Product>();

        public virtual ICollection<Kategori> Kategoriler { get; set; } =
            new List<Kategori>();
    }
}
