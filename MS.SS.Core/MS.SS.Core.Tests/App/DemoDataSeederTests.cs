using Microsoft.Extensions.Logging.Abstractions;
using MS.SS.Core.App.Seeding;
using MS.SS.Core.Modules.Identity.Application.Interfaces;
using MS.SS.Core.Modules.Identity.Domain.Entities;
using MS.SS.Core.Security.Config;
using MS.SS.Core.Security.Interfaces;
using MS.SS.Core.SharedKernel.Interfaces;
using NSubstitute;

namespace MS.SS.Core.Tests.App;

public class DemoDataSeederTests
{
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IPasswordService _passwords = Substitute.For<IPasswordService>();

    private DemoDataSeeder Seeder => new(_users, _unitOfWork, _passwords, NullLogger<DemoDataSeeder>.Instance);

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
        _users.UsernameExistsAsync(DemoUsers.OperatorUsername, Arg.Any<CancellationToken>()).Returns(true);

        await Seeder.SeedAsync(default);

        _users.DidNotReceive().Add(Arg.Any<User>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
