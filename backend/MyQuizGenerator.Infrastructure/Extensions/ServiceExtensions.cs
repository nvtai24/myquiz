using Microsoft.Extensions.DependencyInjection;
using MyQuizGenerator.Application.Common.Interfaces;
using MyQuizGenerator.Infrastructure.Identity;
using MyQuizGenerator.Infrastructure.Repositories;

namespace MyQuizGenerator.Infrastructure.Extensions;

public static class ServiceExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped(typeof(IRepository<,>), typeof(Repository<,>));
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IAuthService, AuthService>();

        return services;
    }
}
