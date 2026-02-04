using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MyQuizGenerator.Domain.Constants;
using MyQuizGenerator.Infrastructure.Identity;

namespace MyQuizGenerator.Infrastructure.Persistence;

/// <summary>
/// Seeds initial data: default roles and admin user.
/// </summary>
public static class DatabaseSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var services = scope.ServiceProvider;

        var logger = services.GetRequiredService<ILogger<ApplicationDbContext>>();

        try
        {
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = services.GetRequiredService<UserManager<AppUser>>();

            await SeedRolesAsync(roleManager, logger);
            await SeedAdminUserAsync(userManager, logger);

            logger.LogInformation("Database seeding completed successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while seeding the database");
            throw;
        }
    }

    private static async Task SeedRolesAsync(
        RoleManager<IdentityRole> roleManager,
        ILogger logger)
    {
        foreach (var role in Roles.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                var result = await roleManager.CreateAsync(new IdentityRole(role));

                if (result.Succeeded)
                {
                    logger.LogInformation("Created role: {Role}", role);
                }
                else
                {
                    logger.LogError("Failed to create role {Role}: {Errors}",
                        role,
                        string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }
            else
            {
                logger.LogDebug("Role already exists: {Role}", role);
            }
        }
    }

    private static async Task SeedAdminUserAsync(
        UserManager<AppUser> userManager,
        ILogger logger)
    {
        const string adminEmail = "admin@myquizgenerator.com";
        const string adminPassword = "Admin123!";

        var adminUser = await userManager.FindByEmailAsync(adminEmail);

        if (adminUser == null)
        {
            adminUser = new AppUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                FirstName = "System",
                LastName = "Administrator",
                EmailConfirmed = true,
                CreatedAt = DateTime.UtcNow
            };

            var result = await userManager.CreateAsync(adminUser, adminPassword);

            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, Roles.Admin);
                await userManager.AddToRoleAsync(adminUser, Roles.User);

                logger.LogInformation("Created admin user: {Email}", adminEmail);
                logger.LogWarning("DEFAULT ADMIN CREDENTIALS - Email: {Email}, Password: {Password}. CHANGE IN PRODUCTION!",
                    adminEmail, adminPassword);
            }
            else
            {
                logger.LogError("Failed to create admin user: {Errors}",
                    string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }
        else
        {
            logger.LogDebug("Admin user already exists: {Email}", adminEmail);
        }
    }
}
