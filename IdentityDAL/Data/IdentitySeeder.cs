using IdentityServiceDomain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace IdentityDAL.Data;

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

        await context.Database.MigrateAsync();

        // 🔹 ROLES
        string[] roles = ["Admin", "User"];

        foreach (var roleName in roles)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new ApplicationRole
                {
                    Name = roleName,
                    NormalizedName = roleName.ToUpper()
                });

                logger.LogInformation("Created role {Role}", roleName);
            }
        }

        // 🔹 TEST USERS
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

            var result = await userManager.CreateAsync(user, "User@1234");

            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(user, "User");
                logger.LogInformation("Created test user: {Email}", email);
            }
            else
            {
                logger.LogWarning("Failed to create test user {Email}: {Errors}",
                    email,
                    string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }

        // 🔹 ADMIN
        var adminId = Guid.Parse("99999999-9999-9999-9999-999999999999");
        var adminEmail = "admin@admin.com";

        var admin = await userManager.FindByEmailAsync(adminEmail);

        if (admin == null)
        {
            admin = new ApplicationUser
            {
                Id = adminId,
                UserName = "Admin",
                Email = adminEmail,
                EmailConfirmed = true,
                CreatedAt = DateTime.UtcNow
            };

            var result = await userManager.CreateAsync(admin, "Admin@1234");

            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(admin, "Admin");
                logger.LogInformation("Created admin user: {Email}", adminEmail);
            }
            else
            {
                logger.LogWarning("Failed to create admin: {Errors}",
                    string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }

    }
}