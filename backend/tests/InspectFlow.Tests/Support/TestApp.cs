using InspectFlow.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Testcontainers.PostgreSql;

namespace InspectFlow.Tests.Support;

/// <summary>
/// One API instance + one PostgreSQL database shared by all integration tests (tests isolate themselves
/// by creating their own users/companies). Uses TEST_DATABASE_CONNECTION_STRING when set, otherwise
/// starts a disposable PostgreSQL container with Testcontainers (image: TEST_POSTGRES_IMAGE or postgres:16-alpine).
/// </summary>
public sealed class TestApp : IAsyncLifetime
{
    private PostgreSqlContainer? _container;
    private string _database = string.Empty;
    private string _adminConnection = string.Empty;
    public WebApplicationFactory<Program> Factory { get; private set; } = null!;
    public string ConnectionString { get; private set; } = string.Empty;
    public string StoragePath { get; } = Path.Combine(Path.GetTempPath(), "inspectflow-tests-" + Guid.NewGuid().ToString("N"));

    public const long MaxUploadBytes = 1024 * 1024;

    public async Task InitializeAsync()
    {
        var external = Environment.GetEnvironmentVariable("TEST_DATABASE_CONNECTION_STRING");
        if (!string.IsNullOrWhiteSpace(external))
        {
            _adminConnection = external;
        }
        else
        {
            _container = new PostgreSqlBuilder(Environment.GetEnvironmentVariable("TEST_POSTGRES_IMAGE") ?? "postgres:16-alpine").Build();
            await _container.StartAsync();
            _adminConnection = _container.GetConnectionString();
        }

        _database = "inspectflow_test_" + Guid.NewGuid().ToString("N")[..12];
        await using (var conn = new NpgsqlConnection(_adminConnection))
        {
            await conn.OpenAsync();
            await using var cmd = new NpgsqlCommand($"CREATE DATABASE {_database}", conn);
            await cmd.ExecuteNonQueryAsync();
        }
        ConnectionString = new NpgsqlConnectionStringBuilder(_adminConnection) { Database = _database }.ConnectionString;

        // Program reads configuration while building, so settings are passed as environment variables.
        Set("ASPNETCORE_ENVIRONMENT", "Testing");
        Set("DATABASE_CONNECTION_STRING", ConnectionString);
        Set("JWT_SECRET", "test-jwt-secret-0123456789abcdefghijklmnopqrstuvwxyz");
        Set("STORAGE_SIGNING_KEY", "test-storage-signing-key-0123456789abcdefghijklmnop");
        Set("STORAGE_LOCAL_PATH", StoragePath);
        Set("MIGRATE_ON_STARTUP", "true");
        Set("SEED_DEMO_DATA", "false");
        Set("AI_PROVIDER", "Mock");
        Set("OPENAI_API_KEY", "");
        Set("Ai__ProcessInline", "true");
        Set("Background__ExpiryWorkerEnabled", "false");
        Set("RateLimiting__AuthPerMinute", "100000");
        Set("RateLimiting__InvitationPerFiveMinutes", "100000");
        Set("RateLimiting__PublicPerMinute", "100000");
        Set("Inspections__MaxUploadBytes", MaxUploadBytes.ToString(System.Globalization.CultureInfo.InvariantCulture));

        Factory = new WebApplicationFactory<Program>().WithWebHostBuilder(b => b.UseEnvironment("Testing"));
        _ = Factory.Server; // start (runs migrations)
    }

    public ApiClient CreateClient() => new(Factory.CreateClient());

    public AppDbContext CreateDbContext() => Factory.Services.CreateScope().ServiceProvider.GetRequiredService<AppDbContext>();

    public async Task DisposeAsync()
    {
        await Factory.DisposeAsync();
        NpgsqlConnection.ClearAllPools();
        if (_container is not null)
        {
            await _container.DisposeAsync();
        }
        else
        {
            await using var conn = new NpgsqlConnection(_adminConnection);
            await conn.OpenAsync();
            await using var cmd = new NpgsqlCommand($"DROP DATABASE IF EXISTS {_database} WITH (FORCE)", conn);
            await cmd.ExecuteNonQueryAsync();
        }
        try { Directory.Delete(StoragePath, recursive: true); } catch (IOException) { } catch (UnauthorizedAccessException) { }
    }

    private static void Set(string key, string value) => Environment.SetEnvironmentVariable(key, value);
}

[CollectionDefinition(Name)]
public sealed class ApiCollection : ICollectionFixture<TestApp>
{
    public const string Name = "api";
}
