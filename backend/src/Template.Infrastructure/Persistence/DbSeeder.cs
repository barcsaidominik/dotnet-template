using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
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
}
