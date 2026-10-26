using MediCubes.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace MediCubes.Web.Data;

public class HospitalDbContext(DbContextOptions<HospitalDbContext> options) : DbContext(options)
{
    public DbSet<Medicine> Medicines => Set<Medicine>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Medicine>(e =>
        {
            e.HasIndex(m => m.Sku).IsUnique();
            e.Property(m => m.UnitPrice).HasPrecision(18, 2);
            e.Ignore(m => m.IsLowStock);
        });
    }
}
