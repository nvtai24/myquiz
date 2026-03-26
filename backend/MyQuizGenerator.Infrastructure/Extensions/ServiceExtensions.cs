using Microsoft.Extensions.DependencyInjection;
using MyQuizGenerator.Application.Common.Interfaces;
using MyQuizGenerator.Application.Common.Interfaces.Repositories;
using MyQuizGenerator.Infrastructure.Identity;
using MyQuizGenerator.Infrastructure.Repositories;
using MyQuizGenerator.Infrastructure.Persistence.Repositories;
using MyQuizGenerator.Infrastructure.Services;
using Amazon.S3;
using Amazon;
using Microsoft.Extensions.Options;
using MyQuizGenerator.Infrastructure.Settings;
using MyQuizGenerator.Application.Common.Services;

namespace MyQuizGenerator.Infrastructure.Extensions;

public static class ServiceExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddHttpClient();

        // Repositories
        services.AddScoped(typeof(IRepository<,>), typeof(Repository<,>));
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IDeckRepository, DeckRepository>();
        services.AddScoped<IQuizAttemptRepository, QuizAttemptRepository>();
        services.AddScoped<ISubscriptionPlanRepository, SubscriptionPlanRepository>();
        services.AddScoped<IDeckRatingRepository, DeckRatingRepository>();

        // Services
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IUserService, UserService>();
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
        services.AddScoped<IDocumentService, DocumentService>();
        services.AddScoped<IPdfService, PdfService>();
        services.AddScoped<IAiService, OpenAiService>();

        return services;
    }
}
