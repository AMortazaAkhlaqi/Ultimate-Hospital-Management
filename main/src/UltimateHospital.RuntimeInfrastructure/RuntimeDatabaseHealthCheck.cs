using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Npgsql;
using Volo.Abp.EntityFrameworkCore;
using Volo.Abp.Uow;

namespace UltimateHospital.RuntimeInfrastructure;

public sealed class RuntimeDatabaseHealthCheck(
    IDbContextProvider<RuntimeDbContext> dbContextProvider,
    IUnitOfWorkManager unitOfWorkManager) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        using var unitOfWork = unitOfWorkManager.Begin(requiresNew: true, isTransactional: false);
        try
        {
            var database = (await dbContextProvider.GetDbContextAsync()).Database;
            if (!await database.CanConnectAsync(cancellationToken))
            {
                return HealthCheckResult.Unhealthy("PostgreSQL is unavailable.");
            }

            var pending = await database.GetPendingMigrationsAsync(cancellationToken);
            var applied = await database.GetAppliedMigrationsAsync(cancellationToken);
            if (pending.Any() || !applied.Any())
            {
                return HealthCheckResult.Unhealthy("Runtime migrations have not been applied.");
            }

            await unitOfWork.CompleteAsync(cancellationToken);
            return HealthCheckResult.Healthy();
        }
        catch (NpgsqlException)
        {
            return HealthCheckResult.Unhealthy("PostgreSQL is unavailable.");
        }
        catch (TimeoutException)
        {
            return HealthCheckResult.Unhealthy("PostgreSQL readiness timed out.");
        }
    }
}
