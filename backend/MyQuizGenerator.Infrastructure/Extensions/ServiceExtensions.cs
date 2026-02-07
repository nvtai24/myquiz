using Microsoft.Extensions.DependencyInjection;
using MyQuizGenerator.Application.Common.Interfaces;
using MyQuizGenerator.Infrastructure.Identity;
using MyQuizGenerator.Infrastructure.Repositories;
using MyQuizGenerator.Infrastructure.Services;
using Amazon.S3;
using Amazon;
using Microsoft.Extensions.Options;
using MyQuizGenerator.Infrastructure.Settings;

namespace MyQuizGenerator.Infrastructure.Extensions;

public static class ServiceExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped(typeof(IRepository<,>), typeof(Repository<,>));
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddTransient<IEmailService, EmailService>();

        services.AddScoped<IAmazonS3>(sp =>
        {
            var settings = sp.GetRequiredService<IOptions<StorageSettings>>().Value;
            var config = new AmazonS3Config
            {
                RegionEndpoint = RegionEndpoint.GetBySystemName(settings.Region)
            };
            return new AmazonS3Client(settings.AccessKey, settings.SecretKey, config);
        });

        services.AddScoped<IFileService, S3FileService>();

        return services;
    }
}
