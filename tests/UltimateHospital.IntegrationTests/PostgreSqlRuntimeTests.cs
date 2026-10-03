using System.Net;
using Aspire.Hosting.Testing;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using UltimateHospital.RuntimeInfrastructure;

namespace UltimateHospital.IntegrationTests;

public sealed class PostgreSqlRuntimeTests(RuntimeFixture fixture) : IClassFixture<RuntimeFixture>
{
    [Fact]
    public async Task RealProviderConnectsToThePinnedPostgreSqlServerAsync()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        await using var context = CreateContext();
        Assert.Equal("Npgsql.EntityFrameworkCore.PostgreSQL", context.Database.ProviderName);
        Assert.True(await context.Database.CanConnectAsync(timeout.Token));
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync(timeout.Token);
        await using var version = new NpgsqlCommand("SHOW server_version_num", connection);
        Assert.Equal("180006", await version.ExecuteScalarAsync(timeout.Token));
    }

    [Fact]
    public async Task DedicatedMigratorAppliesInitialMigrationAndRerunIsIdempotentAsync()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        await using (var context = CreateContext())
        {
            var applied = (await context.Database.GetAppliedMigrationsAsync(timeout.Token)).ToArray();
            Assert.Single(applied);
            Assert.EndsWith("_InitialRuntime", applied[0], StringComparison.Ordinal);
            Assert.Empty(await context.Database.GetPendingMigrationsAsync(timeout.Token));
            await context.Database.MigrateAsync(timeout.Token);
            Assert.Equal(applied, await context.Database.GetAppliedMigrationsAsync(timeout.Token));
        }

        // Independent SQL on a fresh connection verifies durable history, not EF's in-memory model.
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync(timeout.Token);
        await using var history = new NpgsqlCommand("SELECT count(*) FROM runtime.\"__EFMigrationsHistory\"", connection);
        Assert.Equal(1L, await history.ExecuteScalarAsync(timeout.Token));
        await using var businessTables = new NpgsqlCommand(
            "SELECT count(*) FROM information_schema.tables WHERE table_schema IN ('runtime', 'public') AND table_name <> '__EFMigrationsHistory'",
            connection);
        Assert.Equal(0L, await businessTables.ExecuteScalarAsync(timeout.Token));
    }

    [Fact]
    public async Task PostgreSqlRoundTripUsesAConnectionOwnedTemporaryTableAsync()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync(timeout.Token);
        await using (var create = new NpgsqlCommand("CREATE TEMPORARY TABLE m1_probe (value uuid NOT NULL)", connection))
        {
            await create.ExecuteNonQueryAsync(timeout.Token);
        }
        var expected = Guid.NewGuid();
        await using (var insert = new NpgsqlCommand("INSERT INTO m1_probe (value) VALUES (@value)", connection))
        {
            insert.Parameters.AddWithValue("value", expected);
            Assert.Equal(1, await insert.ExecuteNonQueryAsync(timeout.Token));
        }
        await using var read = new NpgsqlCommand("SELECT value FROM m1_probe", connection);
        Assert.Equal(expected, await read.ExecuteScalarAsync(timeout.Token));
    }

    [Theory]
    [InlineData("/alive")]
    [InlineData("/health")]
    public async Task HostExposesLivenessAndDatabaseReadinessAsync(string path)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        using var client = fixture.Application.CreateHttpClient("api", "http");
        using var response = await client.GetAsync(path, timeout.Token);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync(timeout.Token));
    }

    private RuntimeDbContext CreateContext() => new(new DbContextOptionsBuilder<RuntimeDbContext>()
        .UseNpgsql(fixture.ConnectionString, postgres =>
            postgres.MigrationsHistoryTable(RuntimeDbContext.MigrationHistoryTable, RuntimeDbContext.Schema)).Options);
}
