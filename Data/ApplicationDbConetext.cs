using Microsoft.EntityFrameworkCore;
using InventorySystem.Models;
using InventorySystem.Controllers;

namespace InventorySystem.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public DbSet<AdminController> Admins { get; set; }
    }
}
