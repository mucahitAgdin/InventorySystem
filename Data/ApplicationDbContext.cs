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
                e.Property(x => x.Barcode).HasMaxLength(200).IsRequired();
                e.Property(x => x.SerialNumber).HasMaxLength(150);
                e.Property(x => x.Brand).HasMaxLength(100);
                e.Property(x => x.Model).HasMaxLength(150);
                e.Property(x => x.ProductType).HasMaxLength(100);
                e.Property(x => x.Location).HasMaxLength(100);
                e.Property(x => x.CurrentHolder).HasMaxLength(200);

                // 1) Barcode benzersiz: önceki .HasIndex(...).IsUnique() migration'ların varsa kalabilir,
                //    fakat FK için "Alternate Key" şart. (Unique constraint + PrincipalKey)
                e.HasAlternateKey(p => p.Barcode)
                 .HasName("AK_Products_Barcode");

                // (İstersen aşağıdaki satırı kaldır: AlternateKey zaten unique index üretir)
                // e.HasIndex(x => x.Barcode).IsUnique();

                // 2) IsInStock computed (Quantity > 0)
                e.Property(p => p.IsInStock)
                 .HasComputedColumnSql(
                     "CASE WHEN [Quantity] > 0 THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END",
                     stored: true);
            });

            // --- StockTransaction --------------------------------------------
            modelBuilder.Entity<StockTransaction>(e =>
            {
                e.Property(x => x.Barcode).HasMaxLength(100);
                e.Property(x => x.Type).HasConversion<string>().HasMaxLength(10); // enum -> nvarchar
                e.Property(x => x.TransactionDate).HasDefaultValueSql("GETDATE()");
                e.Property(x => x.DeliveredTo).HasMaxLength(200);
                e.Property(x => x.DeliveredBy).HasMaxLength(200);
                e.Property(x => x.Note).HasMaxLength(500);

                e.HasIndex(x => x.Barcode);
                e.HasIndex(x => x.TransactionDate);

                // 3) FK: StockTransaction.Barcode -> Product.Barcode (Alternate Key)
                e.HasOne<Product>()
                 .WithMany()
                 .HasForeignKey(st => st.Barcode)
                 .HasPrincipalKey(p => p.Barcode)
                 .OnDelete(DeleteBehavior.Restrict); // Ürün silinmeden önce log ele alınmalı
            });
        }
    }
}
