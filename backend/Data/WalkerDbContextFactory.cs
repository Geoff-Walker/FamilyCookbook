using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Npgsql;

namespace WalkerFcb.Api.Data;

/// <summary>
/// Design-time factory used by <c>dotnet ef</c> tooling.
/// Provides a WalkerDbContext configured with a local dev connection string
/// so migrations can be generated and applied without running the full application.
/// This class is never instantiated at runtime.
/// </summary>
public class WalkerDbContextFactory : IDesignTimeDbContextFactory<WalkerDbContext>
{
    public WalkerDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? "Host=localhost;Port=5432;Database=walkerfcb;Username=walkerfcb;Password=walkerfcb";

        var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
        dataSourceBuilder.UseVector();
        var dataSource = dataSourceBuilder.Build();

        var optionsBuilder = new DbContextOptionsBuilder<WalkerDbContext>();
        optionsBuilder
            .UseNpgsql(dataSource, o => o.UseVector())
            .UseSnakeCaseNamingConvention();

        return new WalkerDbContext(optionsBuilder.Options);
    }
}
