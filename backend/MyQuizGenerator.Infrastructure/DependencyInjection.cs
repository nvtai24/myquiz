using Microsoft.Extensions.DependencyInjection;

namespace MyQuizGenerator.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        // Register Infrastructure layer services here

        return services;
    }
}
