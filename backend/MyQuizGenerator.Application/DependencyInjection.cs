using Microsoft.Extensions.DependencyInjection;

namespace MyQuizGenerator.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Register Application layer services here

        return services;
    }
}
