using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Template.Domain.Constants;
using Template.Infrastructure.Identity;

namespace Template.Infrastructure.Persistence;

public static class DbSeeder
{
    private const string SYSTEM_ADMIN_EMAIL = "admin@template.io";

    public static async Task SeedRolesAsync(IServiceProvider serviceProvider)
    {
        var roleManager = serviceProvider.GetRequiredService<RoleManager<AppRole>>();

        foreach (var role in new[] { Roles.SYSTEM_ADMIN, Roles.FACILITY_ADMIN, Roles.FACILITY_EDITOR, Roles.FACILITY_VIEWER })
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new AppRole(role));
            }
        }
    }

    public static async Task SeedSystemAdminAsync(IServiceProvider serviceProvider)
    {
        var userManager = serviceProvider.GetRequiredService<UserManager<AppUser>>();
        var logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(DbSeeder));

        var systemAdmins = await userManager.GetUsersInRoleAsync(Roles.SYSTEM_ADMIN);
        if (systemAdmins.Count > 0)
        {
            return;
        }

        var user = await userManager.FindByEmailAsync(SYSTEM_ADMIN_EMAIL);
        if (user is null)
        {
            user = new AppUser
            {
                UserName = SYSTEM_ADMIN_EMAIL,
                Email = SYSTEM_ADMIN_EMAIL,
                IsApproved = true,
                RequiresPasswordChange = true,
                FacilityId = null
            };

            var createResult = await userManager.CreateAsync(user);
            if (!createResult.Succeeded)
            {
                throw new InvalidOperationException($"Nem sikerült létrehozni az alapértelmezett admin felhasználót: {string.Join(", ", createResult.Errors.Select(x => x.Description))}");
            }
        }
        else
        {
            user.IsApproved = true;
            user.RequiresPasswordChange = true;
            user.FacilityId = null;

            var updateResult = await userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                throw new InvalidOperationException($"Nem sikerült frissíteni az alapértelmezett admin felhasználót: {string.Join(", ", updateResult.Errors.Select(x => x.Description))}");
            }
        }

        if (!await userManager.IsInRoleAsync(user, Roles.SYSTEM_ADMIN))
        {
            var addRoleResult = await userManager.AddToRoleAsync(user, Roles.SYSTEM_ADMIN);
            if (!addRoleResult.Succeeded)
            {
                throw new InvalidOperationException($"Nem sikerült a SystemAdmin szerepkör hozzárendelése az alapértelmezett adminhoz: {string.Join(", ", addRoleResult.Errors.Select(x => x.Description))}");
            }
        }

        if (user.PasswordHash is null)
        {
            var setupToken = await userManager.GeneratePasswordResetTokenAsync(user);
            logger.LogWarning("Initial SystemAdmin created for {Email}. A setup token was generated - check stdout for the one-time token value.", SYSTEM_ADMIN_EMAIL);
            Console.WriteLine($"[SETUP] SystemAdmin setup token for {SYSTEM_ADMIN_EMAIL}: {setupToken}");
        }
    }
}
