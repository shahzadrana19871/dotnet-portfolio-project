using DigitalWallet.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace DigitalWallet.Api.Data;

public class WalletDbContext(DbContextOptions<WalletDbContext> options) : DbContext(options)
{
    public DbSet<Wallet> Wallets => Set<Wallet>();
    public DbSet<WalletTransaction> Transactions => Set<WalletTransaction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Wallet>(e =>
        {
            e.HasKey(w => w.Id);
            e.HasIndex(w => w.Msisdn).IsUnique();
            e.Property(w => w.Msisdn).HasMaxLength(20).IsRequired();
            e.Property(w => w.Currency).HasMaxLength(3).IsRequired();
            e.Property(w => w.Balance).HasPrecision(18, 2);

            // Optimistic concurrency on PostgreSQL using the xmin system column.
            if (Database.IsNpgsql())
            {
                e.Property<uint>("xmin").IsRowVersion();
            }
        });

        modelBuilder.Entity<WalletTransaction>(e =>
        {
            e.HasKey(t => t.Id);
            e.HasIndex(t => t.Reference).IsUnique();
            e.Property(t => t.Reference).HasMaxLength(64).IsRequired();
            e.Property(t => t.Amount).HasPrecision(18, 2);
        });
    }
}
