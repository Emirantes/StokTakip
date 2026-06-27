using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using StokTakip.Security;

namespace StokTakip.Models
{
    public class AppDbContext : IdentityDbContext<User, IdentityRole<int>, int>
    {
        private readonly ITenantContext? _tenantContext;

        public AppDbContext(
            DbContextOptions<AppDbContext> options,
            ITenantContext? tenantContext = null)
            : base(options)
        {
            _tenantContext = tenantContext;
        }

        protected int CurrentCompanyId => _tenantContext?.CompanyId ?? 0;

        public DbSet<Product> Products => Set<Product>();

        public DbSet<Kategori> Kategoriler => Set<Kategori>();

        public DbSet<Log> Logs => Set<Log>();

        public DbSet<Company> Companies => Set<Company>();

        public DbSet<StockMovement> StockMovements => Set<StockMovement>();

        protected override void OnModelCreating(
            ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<IdentityRole<int>>()
                .HasData(
                    CreateRole(1, RoleNames.Owner),
                    CreateRole(2, RoleNames.Admin),
                    CreateRole(3, RoleNames.Personel));

            modelBuilder.Entity<User>()
                .HasIndex(x => x.UserName)
                .IsUnique();

            modelBuilder.Entity<User>()
                .HasIndex(x => x.Email)
                .IsUnique();

            modelBuilder.Entity<User>()
                .Property(x => x.UserName)
                .HasColumnName("Username")
                .HasMaxLength(50)
                .IsRequired();

            modelBuilder.Entity<User>()
                .Property(x => x.FullName)
                .HasMaxLength(100);

            modelBuilder.Entity<User>()
                .Property(x => x.Role)
                .HasMaxLength(20);

            modelBuilder.Entity<User>()
                .Property(x => x.Email)
                .HasMaxLength(150);

            modelBuilder.Entity<Company>()
                .HasIndex(x => x.CompanyCode)
                .IsUnique();

            modelBuilder.Entity<Product>()
                .HasIndex(x => x.CompanyId);

            modelBuilder.Entity<Product>()
                .HasIndex(x => x.CreatedByUserId);

            modelBuilder.Entity<Kategori>()
                .HasIndex(x => x.CompanyId);

            modelBuilder.Entity<Log>()
                .HasIndex(x => x.CompanyId);

            modelBuilder.Entity<Log>()
                .HasIndex(x => x.UserId);

            modelBuilder.Entity<StockMovement>()
                .HasIndex(x => x.CompanyId);

            modelBuilder.Entity<StockMovement>()
                .HasIndex(x => x.UserId);

            modelBuilder.Entity<Product>()
            .HasQueryFilter(x =>
            CurrentCompanyId == 0 ||
            x.CompanyId == CurrentCompanyId);

            modelBuilder.Entity<Kategori>()
                .HasQueryFilter(x =>
                    CurrentCompanyId == 0 ||
                    x.CompanyId == CurrentCompanyId);

            modelBuilder.Entity<Log>()
                .HasQueryFilter(x =>
                    CurrentCompanyId == 0 ||
                    x.CompanyId == CurrentCompanyId);

            modelBuilder.Entity<StockMovement>()
                .HasQueryFilter(x =>
                    CurrentCompanyId == 0 ||
                    x.CompanyId == CurrentCompanyId);

            modelBuilder.Entity<User>()
                .HasOne(x => x.Company)
                .WithMany(x => x.Users)
                .HasForeignKey(x => x.CompanyId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Product>()
                .HasOne(x => x.Company)
                .WithMany(x => x.Products)
                .HasForeignKey(x => x.CompanyId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Product>()
                .HasOne(x => x.User)
                .WithMany(x => x.Products)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Product>()
                .HasOne(x => x.CreatedByUser)
                .WithMany()
                .HasForeignKey(x => x.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Product>()
                .HasOne(x => x.Kategori)
                .WithMany(x => x.Products)
                .HasForeignKey(x => x.KategoriId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<StockMovement>()
                .HasOne(x => x.Product)
                .WithMany()
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Log>()
                .HasOne(x => x.Company)
                .WithMany()
                .HasForeignKey(x => x.CompanyId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Log>()
                .HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Kategori>()
                .HasOne(x => x.Company)
                .WithMany(x => x.Kategoriler)
                .HasForeignKey(x => x.CompanyId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<StockMovement>()
                .HasOne(x => x.Company)
                .WithMany()
                .HasForeignKey(x => x.CompanyId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<StockMovement>()
                .HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        }

        private static IdentityRole<int> CreateRole(int id, string name)
        {
            return new IdentityRole<int>
            {
                Id = id,
                Name = name,
                NormalizedName = name.ToUpperInvariant()
            };
        }
    }
}
