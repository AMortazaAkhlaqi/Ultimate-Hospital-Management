using Autofac;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using UltimateHospital.DbMigrator;
using UltimateHospital.RuntimeInfrastructure;
using UltimateHospital.ServiceDefaults;
using Volo.Abp;
using Volo.Abp.Autofac;
using Volo.Abp.EntityFrameworkCore;
using Volo.Abp.Uow;

var builder = Host.CreateApplicationBuilder(args);
// ABP's UseAutofac extension targets IHostBuilder; this host uses the modern builder API.
var containerBuilder = new ContainerBuilder();
builder.Services.AddObjectAccessor(containerBuilder);
builder.ConfigureContainer(new AbpAutofacServiceProviderFactory(containerBuilder));
builder.AddServiceDefaults();
await builder.Services.AddApplicationAsync<UltimateHospitalDbMigratorModule>();
using var host = builder.Build();
var application = host.Services.GetRequiredService<IAbpApplicationWithExternalServiceProvider>();
await application.InitializeAsync(host.Services);
await host.StartAsync();

try
{
    using var scope = host.Services.CreateScope();
    var unitOfWorkManager = scope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();
    using var unitOfWork = unitOfWorkManager.Begin(requiresNew: true, isTransactional: false);
    var provider = scope.ServiceProvider.GetRequiredService<IDbContextProvider<RuntimeDbContext>>();
    var database = (await provider.GetDbContextAsync()).Database;
    using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(2));
    await database.MigrateAsync(timeout.Token);
    await unitOfWork.CompleteAsync(timeout.Token);
    Console.WriteLine("Runtime PostgreSQL migrations applied; no business-module migrations are registered.");
}
finally
{
    await application.ShutdownAsync();
    await host.StopAsync();
}
