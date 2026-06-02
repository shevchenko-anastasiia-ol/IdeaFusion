using IdentityServiceDomain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;

namespace IdentityDAL.Data;

[ExcludeFromCodeCoverage]
public class IdentitySeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();

        var logger = scope.ServiceProvider
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("IdentitySeeder");

        var context = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();

        if (context.Database.IsRelational())
            await context.Database.MigrateAsync();
        else
            await context.Database.EnsureCreatedAsync();
        await SeedRolesAsync(roleManager, logger);
        await SeedTestUsersAsync(userManager, logger);
        await SeedAdminAsync(userManager, logger);
    }

    internal static async Task SeedRolesAsync(RoleManager<ApplicationRole> roleManager, ILogger logger)
    {
        string[] roles = ["Admin", "User"];

        foreach (var roleName in roles)
        {
            if (await roleManager.RoleExistsAsync(roleName))
                continue;

            await roleManager.CreateAsync(new ApplicationRole
            {
                Name = roleName,
                NormalizedName = roleName.ToUpper()
            });

            logger.LogInformation("Created role {Role}", roleName);
        }
    }

    internal static async Task SeedTestUsersAsync(UserManager<ApplicationUser> userManager, ILogger logger)
    {
        var testUsers = new List<(Guid Id, string Email, string Username)>
        {
            (Guid.Parse("22222222-2222-2222-2222-222222222222"), "anna@ideafusion.com", "AnnoGray"),
            (Guid.Parse("44444444-4444-4444-4444-444444444444"), "vlad@ideafusion.com", "VladBibber"),
            (Guid.Parse("66666666-6666-6666-6666-666666666666"), "emily@ideafusion.com", "EmilyFox")
        };

        foreach (var (id, email, username) in testUsers)
        {
            if (await userManager.FindByEmailAsync(email) is not null)
                continue;

            var user = new ApplicationUser
            {
                Id = id,
                UserName = username,
                Email = email,
                EmailConfirmed = true,
                CreatedAt = DateTime.UtcNow
            };

            await CreateUserAsync(userManager, user, "User@1234", "User", logger);
        }
    }

    internal static async Task SeedAdminAsync(UserManager<ApplicationUser> userManager, ILogger logger)
    {
        var adminEmail = "admin@admin.com";

        if (await userManager.FindByEmailAsync(adminEmail) is not null)
            return;

        var admin = new ApplicationUser
        {
            Id = Guid.Parse("99999999-9999-9999-9999-999999999999"),
            UserName = "Admin",
            Email = adminEmail,
            EmailConfirmed = true,
            CreatedAt = DateTime.UtcNow
        };

        await CreateUserAsync(userManager, admin, "Admin@1234", "Admin", logger);
    }

    internal static async Task CreateUserAsync(
        UserManager<ApplicationUser> userManager,
        ApplicationUser user,
        string password,
        string role,
        ILogger logger)
    {
        var result = await userManager.CreateAsync(user, password);

        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(user, role);
            logger.LogInformation("Created user: {Email}", user.Email);
        }
        else
        {
            logger.LogWarning("Failed to create user {Email}: {Errors}",
                user.Email,
                string.Join(", ", result.Errors.Select(e => e.Description)));
        }
    }
}
