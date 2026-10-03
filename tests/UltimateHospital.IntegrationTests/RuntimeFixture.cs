using Aspire.Hosting;
using Aspire.Hosting.Testing;

namespace UltimateHospital.IntegrationTests;

public sealed class RuntimeFixture : IAsyncLifetime
{
    private DistributedApplication? _application;

    public DistributedApplication Application => _application
        ?? throw new InvalidOperationException("AppHost has not been started.");

    public string ConnectionString { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(4));
        var builder = await DistributedApplicationTestingBuilder.CreateAsync<Projects.UltimateHospital_AppHost>(timeout.Token);
        _application = await builder.BuildAsync(timeout.Token);
        await _application.StartAsync(timeout.Token);
        await _application.ResourceNotifications.WaitForResourceHealthyAsync("api", timeout.Token);
        ConnectionString = await _application.GetConnectionStringAsync("hospitaldb", timeout.Token)
            ?? throw new InvalidOperationException("Aspire did not supply hospitaldb configuration.");
    }

    public async Task DisposeAsync()
    {
        if (_application is not null)
        {
            await _application.DisposeAsync();
        }
    }
}
