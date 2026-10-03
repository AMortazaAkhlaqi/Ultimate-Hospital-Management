using Microsoft.EntityFrameworkCore;
using Volo.Abp.Data;
using Volo.Abp.EntityFrameworkCore;

namespace UltimateHospital.RuntimeInfrastructure;

[ConnectionStringName(ConnectionName)]
public sealed class RuntimeDbContext(DbContextOptions<RuntimeDbContext> options)
    : AbpDbContext<RuntimeDbContext>(options)
{
    public const string ConnectionName = "hospitaldb";
    public const string Schema = "runtime";
    public const string MigrationHistoryTable = "__EFMigrationsHistory";

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        // This context owns technical migration metadata only, never business entities.
        modelBuilder.HasDefaultSchema(Schema);
    }
}
