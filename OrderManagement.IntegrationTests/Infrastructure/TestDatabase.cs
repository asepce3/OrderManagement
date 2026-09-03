using Microsoft.EntityFrameworkCore;
using Npgsql;
using OrderManagement.Data;

namespace OrderManagement.IntegrationTests.Infrastructure;

public sealed class TestDatabase : IAsyncDisposable
{
    public string ConnectionString { get; }

    public string DatabaseName { get; }

    private readonly string _adminConnectionString;

    public TestDatabase()
    {
        var host = Env("OM_TEST_PGHOST", "localhost");
        var port = Env("OM_TEST_PGPORT", "5432");
        var username = Env("OM_TEST_PGUSER", "postgres");
        var password = Env("OM_TEST_PGPASSWORD", "root");

        DatabaseName = $"{Env("OM_TEST_PGDATABASE_PREFIX", "order_management_test")}_{Guid.NewGuid():N}";
        ConnectionString = Build(host, port, username, password, DatabaseName);
        _adminConnectionString = Build(host, port, username, password, "postgres") + ";Pooling=false";
    }

    public async Task InitializeAsync()
    {
        await ExecuteAdminAsync($"DROP DATABASE IF EXISTS \"{DatabaseName}\"");
        await ExecuteAdminAsync($"CREATE DATABASE \"{DatabaseName}\"");

        await using var context = CreateDbContext();
        await context.Database.MigrateAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await ExecuteAdminAsync(
            $"SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname = '{DatabaseName}' AND pid <> pg_backend_pid()");
        await ExecuteAdminAsync($"DROP DATABASE IF EXISTS \"{DatabaseName}\"");
    }

    public AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;

        return new AppDbContext(options);
    }

    private async Task ExecuteAdminAsync(string sql)
    {
        await using var connection = new NpgsqlConnection(_adminConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    private static string Build(string host, string port, string username, string password, string database)
        => $"Host={host};Port={port};Database={database};Username={username};Password={password}";

    private static string Env(string name, string fallback)
        => Environment.GetEnvironmentVariable(name) is { Length: > 0 } value ? value : fallback;
}
