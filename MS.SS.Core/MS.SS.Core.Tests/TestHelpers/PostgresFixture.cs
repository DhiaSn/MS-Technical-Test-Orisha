using Microsoft.EntityFrameworkCore;
using MS.SS.Core.Infrastructure.Database.Context;
using Npgsql;
using Testcontainers.PostgreSql;

namespace MS.SS.Core.Tests.TestHelpers;

public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:17-alpine").Build();

    public string ConnectionString => _container.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        await using var context = CreateContext();
        await context.Database.MigrateAsync();
    }

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();

    public AppDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
                .UseNpgsql(ConnectionString)
                .UseSnakeCaseNamingConvention()
                .Options,
            TimeProvider.System);

    /// <summary>Creates an empty, uniquely named database on the shared container and returns its connection string.</summary>
    public string CreateDatabase()
    {
        var name = $"test_{Guid.NewGuid():N}";

        using (var connection = new NpgsqlConnection(ConnectionString))
        {
            connection.Open();
            using var command = new NpgsqlCommand($"create database \"{name}\"", connection);
            command.ExecuteNonQuery();
        }

        return new NpgsqlConnectionStringBuilder(ConnectionString) { Database = name }.ConnectionString;
    }
}
