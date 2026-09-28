using Microsoft.EntityFrameworkCore;
using MS.SS.Core.Infrastructure.Database.Config;
using MS.SS.Core.Infrastructure.Database.Context;
using MS.SS.Core.Modules.Identity.Domain.Entities;
using MS.SS.Core.Modules.Identity.Infrastructure.Repositories;
using MS.SS.Core.Security.Config;
using MS.SS.Core.Tests.TestHelpers;
using Npgsql;

namespace MS.SS.Core.Tests.Modules.Identity.Infrastructure;

[Collection(PostgresCollection.Name)]
public sealed class UserPersistenceTests(PostgresFixture postgres) : IAsyncLifetime
{
    public async Task InitializeAsync()
    {
        await using var context = NewContext();
        await context.Database.MigrateAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Migration_creates_the_users_table_in_the_identity_schema()
    {
        await using var connection = new NpgsqlConnection(postgres.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            "SELECT count(*) FROM information_schema.tables WHERE table_schema = @schema AND table_name = 'users'", connection);
        command.Parameters.AddWithValue("schema", ModuleSchemas.Identity);

        Assert.Equal(1L, await command.ExecuteScalarAsync());
    }

    [Fact]
    public async Task A_user_round_trips()
    {
        var user = User.Register(UniqueUsername(), "Magasinier", "hash", IdentityRoles.Operator);
        await using (var context = NewContext())
        {
            new UserRepository(context).Add(user);
            await context.SaveChangesAsync();
        }

        await using var reload = NewContext();
        var loaded = await new UserRepository(reload).FindByIdAsync(user.Id, default);

        Assert.NotNull(loaded);
        Assert.Equal(user.Username, loaded.Username);
        Assert.Equal("Magasinier", loaded.DisplayName);
        Assert.Equal(IdentityRoles.Operator, loaded.Role);
        Assert.True(loaded.IsActive);
        Assert.Null(loaded.LastLoginAt);
        Assert.NotEqual(default, loaded.CreatedAt);
    }

    [Fact]
    public async Task A_user_saved_as_inactive_stays_inactive()
    {
        var user = User.Register(UniqueUsername(), "Magasinier", "hash", IdentityRoles.Operator);
        user.Deactivate();
        await using (var context = NewContext())
        {
            new UserRepository(context).Add(user);
            await context.SaveChangesAsync();
        }

        await using var reload = NewContext();
        var loaded = await new UserRepository(reload).FindByIdAsync(user.Id, default);

        Assert.False(loaded!.IsActive);
    }

    [Fact]
    public async Task Two_users_cannot_share_a_username()
    {
        var username = UniqueUsername();
        await using var context = NewContext();
        var repository = new UserRepository(context);
        repository.Add(User.Register(username, "First", "hash", IdentityRoles.Operator));
        await context.SaveChangesAsync();

        await using var second = NewContext();
        new UserRepository(second).Add(User.Register(username, "Second", "hash", IdentityRoles.Operator));

        var failure = await Assert.ThrowsAsync<DbUpdateException>(() => second.SaveChangesAsync());
        var postgresError = Assert.IsType<PostgresException>(failure.InnerException);
        Assert.Equal("23505", postgresError.SqlState);
        Assert.Equal("ux_users_username", postgresError.ConstraintName);
    }

    [Fact]
    public async Task Lookups_by_username_and_existence_check_agree()
    {
        var username = UniqueUsername();
        await using (var context = NewContext())
        {
            new UserRepository(context).Add(User.Register(username, "Magasinier", "hash", IdentityRoles.Operator));
            await context.SaveChangesAsync();
        }

        await using var reload = NewContext();
        var repository = new UserRepository(reload);

        Assert.NotNull(await repository.FindByUsernameAsync(username, default));
        Assert.True(await repository.UsernameExistsAsync(username, default));
        Assert.Null(await repository.FindByUsernameAsync(UniqueUsername(), default));
        Assert.False(await repository.UsernameExistsAsync(UniqueUsername(), default));
    }

    [Fact]
    public async Task A_login_recorded_on_a_tracked_user_is_saved()
    {
        var username = UniqueUsername();
        await using (var context = NewContext())
        {
            new UserRepository(context).Add(User.Register(username, "Magasinier", "hash", IdentityRoles.Operator));
            await context.SaveChangesAsync();
        }

        var at = new DateTime(2026, 9, 28, 8, 0, 0, DateTimeKind.Utc);
        await using (var context = NewContext())
        {
            var tracked = await new UserRepository(context).FindByUsernameAsync(username, default);
            tracked!.RecordLogin(at);
            await context.SaveChangesAsync();
        }

        await using var reload = NewContext();
        var loaded = await new UserRepository(reload).FindByUsernameAsync(username, default);
        Assert.Equal(at, loaded!.LastLoginAt);
    }

    private static string UniqueUsername() => $"user-{Guid.NewGuid():N}";

    private AppDbContext NewContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(postgres.ConnectionString, npgsql =>
                npgsql.MigrationsAssembly(typeof(AppDbContext).Assembly.GetName().Name))
            .UseSnakeCaseNamingConvention()
            .Options;

        return new AppDbContext(options, TimeProvider.System);
    }
}
