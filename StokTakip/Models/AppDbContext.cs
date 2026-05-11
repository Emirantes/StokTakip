using Microsoft.EntityFrameworkCore;

namespace StokTakip.Models
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<Product> Products { get; set; }

        public DbSet<Kategori> Kategoriler { get; set; }

        public DbSet<User> Users { get; set; }

        public DbSet<Log> Logs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Username)
                .IsUnique();

            // PRODUCT -> USER İLİŞKİSİ
            modelBuilder.Entity<Product>()
                .HasOne(p => p.User)
                .WithMany(u => u.Products)
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}