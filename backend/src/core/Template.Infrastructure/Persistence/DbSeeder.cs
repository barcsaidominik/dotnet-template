using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Template.Application.Common.Interfaces;
using Template.Application.Common.Notifications;
using Template.Domain.Constants;
using Template.Infrastructure.Identity;

namespace Template.Infrastructure.Persistence;

public static class DbSeeder
{
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
        var notificationService = serviceProvider.GetRequiredService<INotificationService>();
        var frontendSettings = serviceProvider.GetRequiredService<IFrontendSettings>();
        var logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(DbSeeder));
        var configuration = serviceProvider.GetRequiredService<IConfiguration>();
        var systemAdminEmail = configuration.GetValue<string>("SystemAdmin:Email") ?? "admin@template.io";

        var systemAdmins = await userManager.GetUsersInRoleAsync(Roles.SYSTEM_ADMIN);
        if (systemAdmins.Count > 0)
        {
            return;
        }

        var user = await userManager.FindByEmailAsync(systemAdminEmail);
        if (user is null)
        {
            user = new AppUser
            {
                UserName = systemAdminEmail,
                Email = systemAdminEmail,
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
            var resetLink = $"{frontendSettings.BaseUrl}/auth/set-password?token={Uri.EscapeDataString(setupToken)}&email={Uri.EscapeDataString(systemAdminEmail)}";

            var notification = new NotificationRequest(
                NotificationTemplateKey.PasswordReset,
                new NotificationRecipient(systemAdminEmail),
                new PasswordResetNotificationModel(resetLink),
                null,
                [NotificationChannelType.Email]);

            try
            {
                await notificationService.SendAsync(notification);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to send setup email for initial SystemAdmin ({Email}). Trigger forgot-password manually.", systemAdminEmail);
            }
        }
    }
}
