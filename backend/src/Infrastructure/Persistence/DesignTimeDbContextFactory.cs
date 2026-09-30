using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace InspectFlow.Infrastructure.Persistence;

/// <summary>Used by `dotnet ef` (migrations). Reads DATABASE_CONNECTION_STRING or falls back to the local docker database.</summary>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var cs = Environment.GetEnvironmentVariable("DATABASE_CONNECTION_STRING")
                 ?? "Host=localhost;Port=5432;Database=inspectflow;Username=inspectflow;Password=inspectflow";
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(cs, o => o.MigrationsHistoryTable("__ef_migrations_history", "public"))
            .UseSnakeCaseNamingConvention()
            .Options;
        return new AppDbContext(options);
    }
}
