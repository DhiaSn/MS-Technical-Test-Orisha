using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MS.SS.Core.App.Seeding;
using MS.SS.Core.Infrastructure.Database.Context;
using MS.SS.Core.Modules.Identity.Domain.Entities;
using MS.SS.Core.Security.Interfaces;
using MS.SS.Core.Tests.TestHelpers;

namespace MS.SS.Core.Tests.API.Http;

[Collection(PostgresCollection.Name)]
public sealed class DemoOperatorSeedingTests(PostgresFixture postgres)
{
    [Fact]
    public async Task Booting_the_api_stores_the_demo_operator_with_a_hash_that_verifies()
    {
        await using var factory = new ReceptionApiFactory(postgres);

        var operators = await LoadOperatorsAsync(factory);

        var demoOperator = Assert.Single(operators);
        Assert.NotEqual(DemoUsers.OperatorPassword, demoOperator.PasswordHash);
        Assert.DoesNotContain(DemoUsers.OperatorPassword, demoOperator.PasswordHash);

        var passwords = factory.Services.GetRequiredService<IPasswordService>();
        Assert.True(passwords.VerifyPassword(DemoUsers.OperatorPassword, demoOperator.PasswordHash));
    }

    [Fact]
    public async Task Booting_the_api_twice_on_the_same_database_does_not_duplicate_the_operator()
    {
        await using (var first = new ReceptionApiFactory(postgres))
        {
            Assert.Single(await LoadOperatorsAsync(first));
        }

        await using var second = new ReceptionApiFactory(postgres);

        Assert.Single(await LoadOperatorsAsync(second));
    }

    private static async Task<List<User>> LoadOperatorsAsync(ReceptionApiFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        return await context.Set<User>()
            .AsNoTracking()
            .Where(user => user.Username == DemoUsers.OperatorUsername)
            .ToListAsync();
    }
}
