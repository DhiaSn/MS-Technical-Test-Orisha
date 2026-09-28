using Microsoft.Extensions.DependencyInjection;
using MS.SS.Core.Modules.Reception.Application.Interfaces;
using MS.SS.Core.Modules.Reception.Infrastructure.Repositories;

namespace MS.SS.Core.Modules.Reception.Infrastructure.Extensions;

public static class ReceptionServiceExtensions
{
    public static IServiceCollection AddReceptionModule(this IServiceCollection services)
    {
        services.AddScoped<IDeliveryRepository, DeliveryRepository>();

        return services;
    }
}
