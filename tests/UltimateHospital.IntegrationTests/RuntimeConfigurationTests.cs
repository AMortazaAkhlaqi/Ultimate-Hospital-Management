using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using UltimateHospital.RuntimeInfrastructure;
using Volo.Abp.Modularity;

namespace UltimateHospital.IntegrationTests;

public sealed class RuntimeConfigurationTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void MissingDatabaseConfigurationFailsInsteadOfSelectingAFallback(string? connectionString)
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?> { ["ConnectionStrings:hospitaldb"] = connectionString }).Build();
        services.AddSingleton<IConfiguration>(configuration);
        var module = new RuntimeInfrastructureModule();
        var exception = Assert.Throws<InvalidOperationException>(() =>
            module.ConfigureServices(new ServiceConfigurationContext(services)));
        Assert.Equal("ConnectionStrings:hospitaldb must be supplied by the runtime environment.", exception.Message);
    }
}
