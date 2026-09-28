using MS.SS.Core.Modules.Identity.Application.Interfaces;
using MS.SS.Core.Modules.Identity.Domain.Entities;
using MS.SS.Core.Security.Config;
using MS.SS.Core.Security.Interfaces;
using MS.SS.Core.SharedKernel.Interfaces;

namespace MS.SS.Core.App.Seeding;

public sealed class DemoDataSeeder(
    IUserRepository users,
    IUnitOfWork unitOfWork,
    IPasswordService passwords,
    ILogger<DemoDataSeeder> logger)
{
    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        if (await users.UsernameExistsAsync(DemoUsers.OperatorUsername, cancellationToken))
        {
            logger.LogInformation("Demo operator already present; skipping.");
            return;
        }

        users.Add(User.Register(
            DemoUsers.OperatorUsername,
            DemoUsers.OperatorDisplayName,
            passwords.HashPassword(DemoUsers.OperatorPassword),
            IdentityRoles.Operator));
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Seeded demo operator {Username}.", DemoUsers.OperatorUsername);
    }
}
