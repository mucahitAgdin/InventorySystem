using Microsoft.EntityFrameworkCore;
using InventorySystem.Models;


namespace InventorySystem.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        // Veritabanındaki Admin tablosu için model tanımı
        public DbSet<Admin> Admins { get; set; }

        // Veritabanındaki Product tablosu için model tanımı 
        public DbSet<Product> Products { get; set; }

        public DbSet<StockTransaction> StockTransaction { get; set; }

        // Eğer Column config veya indexleri Fluent API'dan vermek istersen:
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Admin>(e =>
            {
                e.Property(x => x.Username).HasMaxLength(50).IsRequired();
                e.Property(x => x.Password).IsRequired();
                e.HasIndex(x => x.Username);
            });

            modelBuilder.Entity<Product>(e =>
            {
                e.Property(x => x.Name).HasMaxLength(200);
                e.Property(x => x.Barcode).HasMaxLength(200).IsRequired();
                e.Property(x => x.SerialNumber).HasMaxLength(150);
                e.Property(x => x.Brand).HasMaxLength(100);
                e.Property(x => x.Model).HasMaxLength(150);
                e.Property(x => x.ProductType).HasMaxLength(100);
                e.Property(x => x.Location).HasMaxLength(100);
                e.Property(x => x.CurrentHolder).HasMaxLength(200);
                e.HasIndex(x => x.Barcode)
                .IsUnique();
                e.HasIndex(x => x.SerialNumber)
                .IsUnique(false);
                e.Property(p => p.IsInStock)
                .HasComputedColumnSql("CASE WHEN [Quantity] > 0 THEN 1 ELSE 0 END", stored: true);
            });

            modelBuilder.Entity<StockTransaction>(e =>
            {
                e.Property(x => x.Barcode).HasMaxLength(100);
                e.Property(x => x.Type).HasMaxLength(30);
                e.Property(x => x.TransactionDate)
                    .HasDefaultValueSql("GETDATE()");
                e.HasIndex(x => x.Barcode);
            });
        }
    }
}

///<summary>
/// dotnet ef database drop
/// dotnet ef migrations add InitialCreate_2025
/// dotnet ef database update
/// </summary>