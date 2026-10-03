using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore.PostgreSql;
using Volo.Abp.Modularity;

namespace UltimateHospital.RuntimeInfrastructure;

[DependsOn(typeof(AbpEntityFrameworkCorePostgreSqlModule))]
public sealed class RuntimeInfrastructureModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();
        if (string.IsNullOrWhiteSpace(configuration.GetConnectionString(RuntimeDbContext.ConnectionName)))
        {
            throw new InvalidOperationException("ConnectionStrings:hospitaldb must be supplied by the runtime environment.");
        }

        context.Services.AddAbpDbContext<RuntimeDbContext>();
        Configure<AbpDbContextOptions>(options =>
            options.UseNpgsql<RuntimeDbContext>(postgres =>
                postgres.MigrationsHistoryTable(RuntimeDbContext.MigrationHistoryTable, RuntimeDbContext.Schema)));
    }
}
