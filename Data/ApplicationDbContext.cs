using InventorySystem.Models;
using Microsoft.EntityFrameworkCore;

namespace InventorySystem.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        // Tablolar
        public DbSet<Admin> Admins { get; set; }
        public DbSet<Product> Products { get; set; }

        // Not: property adı tekil; istersen plural (StockTransactions) yapabilirsin.
        public DbSet<StockTransaction> StockTransaction { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // --- Admin --------------------------------------------------------
            modelBuilder.Entity<Admin>(e =>
            {
                e.Property(x => x.Username).HasMaxLength(50).IsRequired();
                e.Property(x => x.Password).IsRequired();
                e.HasIndex(x => x.Username);
            });

            // --- Product ------------------------------------------------------
            modelBuilder.Entity<Product>(e =>
            {
                e.Property(x => x.Name).HasMaxLength(200);
                e.Property(x => x.Barcode).HasMaxLength(7).IsRequired();
                e.Property(x => x.SerialNumber).HasMaxLength(150);
                e.Property(x => x.Brand).HasMaxLength(100);
                e.Property(x => x.Model).HasMaxLength(150);
                e.Property(x => x.ProductType).HasMaxLength(100);
                e.Property(x => x.Location).HasMaxLength(200);
                e.Property(x => x.CurrentHolder).HasMaxLength(200);

                // ✅ Barcode = UNIQUE (Alternate Key; FK için de kullanılabilir)
                e.HasAlternateKey(p => p.Barcode).HasName("AK_Products_Barcode");

                // ✅ SerialNumber = UNIQUE ama NULL olabilir → filtered unique
                e.HasIndex(p => p.SerialNumber)
                 .IsUnique()
                 .HasFilter("[SerialNumber] IS NOT NULL");

                // ✅ IsInStock'ı konuma göre hesapla (Depo=stokta)
                e.Property(p => p.IsInStock)
                 .HasComputedColumnSql(
                    "CASE WHEN COALESCE([Location],'') = 'Depo' THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END",
                    stored: true);
            });

            // --- StockTransaction --------------------------------------------
            modelBuilder.Entity<StockTransaction>(e =>
            {
                e.Property(x => x.Barcode).HasMaxLength(7); // ⬅ Products.Barcode ile birebir
                e.Property(x => x.Type).HasConversion<string>().HasMaxLength(10);
                e.Property(x => x.TransactionDate).HasDefaultValueSql("GETDATE()");
                e.Property(x => x.DeliveredTo).HasMaxLength(200);
                e.Property(x => x.DeliveredBy).HasMaxLength(200);
                e.Property(x => x.Note).HasMaxLength(500);

                e.HasIndex(x => x.Barcode);
                e.HasIndex(x => x.TransactionDate);

                // (opsiyonel) FK: hareketleri de sadece kayıtlı barkoda izin ver
                e.HasOne<Product>()
                 .WithMany()
                 .HasForeignKey(st => st.Barcode)
                 .HasPrincipalKey(p => p.Barcode)
                 .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}
