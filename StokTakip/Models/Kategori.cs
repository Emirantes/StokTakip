using System.Collections.Generic;

namespace StokTakip.Models
{
    public class Kategori
    {
        public int Id { get; set; }

        public required string KategoriAd { get; set; }

        public string? Aciklama { get; set; }

        public List<Product>? Products { get; set; }
    }
}
