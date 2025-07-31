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
    }
}
