using MediatR;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace MyQuizGenerator.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        // MediatR
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));

        // AutoMapper
        services.AddAutoMapper(assembly);

        return services;
    }
}
