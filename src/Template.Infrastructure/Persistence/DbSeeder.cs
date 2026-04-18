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

        foreach (var role in new[] { Roles.SystemAdmin, Roles.FacilityAdmin, Roles.FacilityEditor, Roles.FacilityViewer })
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new AppRole(role));
            }
        }
    }
}
