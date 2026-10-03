using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace UltimateHospital.RuntimeInfrastructure;

public sealed class RuntimeDbContextFactory : IDesignTimeDbContextFactory<RuntimeDbContext>
{
    public RuntimeDbContext CreateDbContext(string[] args)
    {
        // EF code generation needs provider metadata; it does not open this connection.
        var options = new DbContextOptionsBuilder<RuntimeDbContext>()
            .UseNpgsql("Host=localhost;Database=design_time_only", postgres =>
                postgres.MigrationsHistoryTable(RuntimeDbContext.MigrationHistoryTable, RuntimeDbContext.Schema))
            .Options;
        return new RuntimeDbContext(options);
    }
}
