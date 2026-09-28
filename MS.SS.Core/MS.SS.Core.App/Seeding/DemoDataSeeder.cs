using MS.SS.Core.Modules.Identity.Application.Interfaces;
using MS.SS.Core.Modules.Identity.Domain.Entities;
using MS.SS.Core.Modules.Reception.Application.Interfaces;
using MS.SS.Core.Security.Config;
using MS.SS.Core.Security.Interfaces;
using MS.SS.Core.SharedKernel.Interfaces;

namespace MS.SS.Core.App.Seeding;

public sealed class DemoDataSeeder(
    IUserRepository users,
    IDeliveryRepository deliveries,
    IUnitOfWork unitOfWork,
    IPasswordService passwords,
    ILogger<DemoDataSeeder> logger)
{
    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        await SeedOperatorAsync(cancellationToken);
        await SeedDeliveryAsync(cancellationToken);
    }

    private async Task SeedOperatorAsync(CancellationToken cancellationToken)
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

    private async Task SeedDeliveryAsync(CancellationToken cancellationToken)
    {
        if (await deliveries.AnyAsync(cancellationToken))
        {
            logger.LogInformation("Demo delivery already present; skipping.");
            return;
        }

        deliveries.Add(DemoDeliveries.CreateCmd2026());
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Seeded demo delivery CMD-2026.");
    }
}
