using UltimateHospital.HttpApi.Host;
using UltimateHospital.RuntimeInfrastructure;
using UltimateHospital.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseAutofac();
builder.AddServiceDefaults();
builder.Services.AddHealthChecks()
    .AddCheck<RuntimeDatabaseHealthCheck>("postgresql", timeout: TimeSpan.FromSeconds(5));
await builder.AddApplicationAsync<UltimateHospitalHttpApiHostModule>();

await using var application = builder.Build();
await application.InitializeApplicationAsync();
application.MapDefaultEndpoints();
await application.RunAsync();
