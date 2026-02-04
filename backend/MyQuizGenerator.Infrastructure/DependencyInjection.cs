using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MyQuizGenerator.Infrastructure.Extensions;

namespace MyQuizGenerator.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddDatabase(configuration)
            .AddIdentityServices(configuration)
            .AddApplicationServices();

        return services;
    }
}
