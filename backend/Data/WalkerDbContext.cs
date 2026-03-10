using Microsoft.EntityFrameworkCore;

namespace WalkerFcb.Api.Data;

/// <summary>
/// EF Core database context for the WalkerFCB application.
/// Entity definitions are added in WAL-27.
/// </summary>
public class WalkerDbContext : DbContext
{
    public WalkerDbContext(DbContextOptions<WalkerDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // pgvector extension is enabled via migration (WAL-25).
        // Entity configurations and DbSet properties are added in WAL-27.
    }
}
