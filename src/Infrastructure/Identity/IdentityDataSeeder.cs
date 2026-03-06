using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MyHomeSolution.Application.Common.Constants;

namespace MyHomeSolution.Infrastructure.Identity;

public static class IdentityDataSeeder
{
    public static async Task SeedRolesAsync(IServiceProvider serviceProvider)
    {
        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var logger = serviceProvider.GetRequiredService<ILogger<IdentityRole>>();

        foreach (var role in Roles.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                var result = await roleManager.CreateAsync(new IdentityRole(role));

                if (result.Succeeded)
                    logger.LogInformation("Created role '{Role}'", role);
                else
                    logger.LogWarning("Failed to create role '{Role}': {Errors}",
                        role, string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }
    }

    public static async Task SeedDefaultAdminAsync(
        IServiceProvider serviceProvider, string email, string password,
        string firstName, string lastName)
    {
        var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var logger = serviceProvider.GetRequiredService<ILogger<ApplicationUser>>();

        if (await userManager.Users.AnyAsync(u => u.Email == email))
        {
            logger.LogInformation("Default admin user '{Email}' already exists", email);
            return;
        }

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FirstName = firstName,
            LastName = lastName,
            IsActive = true,
            EmailConfirmed = true,
            CreatedAt = DateTimeOffset.UtcNow
        };

        var result = await userManager.CreateAsync(user, password);

        if (!result.Succeeded)
        {
            logger.LogWarning("Failed to create default admin: {Errors}",
                string.Join(", ", result.Errors.Select(e => e.Description)));
            return;
        }

        var roleResult = await userManager.AddToRoleAsync(user, Roles.Administrator);

        if (roleResult.Succeeded)
            logger.LogInformation("Default admin user '{Email}' created and assigned to '{Role}' role",
                email, Roles.Administrator);
        else
            logger.LogWarning("Failed to assign admin role: {Errors}",
                string.Join(", ", roleResult.Errors.Select(e => e.Description)));
    }
}
