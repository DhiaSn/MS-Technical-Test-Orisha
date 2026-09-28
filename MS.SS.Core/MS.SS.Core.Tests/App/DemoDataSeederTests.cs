using Microsoft.Extensions.Logging.Abstractions;
using MS.SS.Core.App.Seeding;
using MS.SS.Core.Modules.Identity.Application.Interfaces;
using MS.SS.Core.Modules.Identity.Domain.Entities;
using MS.SS.Core.Modules.Reception.Application.Interfaces;
using MS.SS.Core.Modules.Reception.Domain.Entities;
using MS.SS.Core.Security.Config;
using MS.SS.Core.Security.Interfaces;
using MS.SS.Core.SharedKernel.Interfaces;
using NSubstitute;

namespace MS.SS.Core.Tests.App;

public class DemoDataSeederTests
{
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IDeliveryRepository _deliveries = Substitute.For<IDeliveryRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IPasswordService _passwords = Substitute.For<IPasswordService>();

    public DemoDataSeederTests()
    {
        _users.UsernameExistsAsync(DemoUsers.OperatorUsername, Arg.Any<CancellationToken>()).Returns(true);
        _deliveries.AnyAsync(Arg.Any<CancellationToken>()).Returns(true);
    }

    private DemoDataSeeder Seeder => new(_users, _deliveries, _unitOfWork, _passwords, NullLogger<DemoDataSeeder>.Instance);

    [Fact]
    public async Task Seeds_the_operator_when_absent()
    {
        _users.UsernameExistsAsync(DemoUsers.OperatorUsername, Arg.Any<CancellationToken>()).Returns(false);
        _passwords.HashPassword(DemoUsers.OperatorPassword).Returns("hashed-by-service");

        await Seeder.SeedAsync(default);

        _users.Received(1).Add(Arg.Is<User>(user =>
            user.Username == DemoUsers.OperatorUsername
            && user.DisplayName == DemoUsers.OperatorDisplayName
            && user.PasswordHash == "hashed-by-service"
            && user.Role == IdentityRoles.Operator));
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Does_nothing_when_the_operator_exists()
    {
        await Seeder.SeedAsync(default);

        _users.DidNotReceive().Add(Arg.Any<User>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Seeds_the_delivery_when_none_exists()
    {
        _deliveries.AnyAsync(Arg.Any<CancellationToken>()).Returns(false);

        await Seeder.SeedAsync(default);

        _deliveries.Received(1).Add(Arg.Is<Delivery>(delivery => delivery.OrderNumber == "CMD-2026"));
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Does_not_reseed_the_delivery()
    {
        await Seeder.SeedAsync(default);

        _deliveries.DidNotReceive().Add(Arg.Any<Delivery>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
