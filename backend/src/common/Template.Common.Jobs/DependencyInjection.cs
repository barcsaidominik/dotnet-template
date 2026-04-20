using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Template.Application.Common.Interfaces;
using TickerQ.Dashboard.DependencyInjection;
using TickerQ.DependencyInjection;
using TickerQ.EntityFrameworkCore.Customizer;
using TickerQ.EntityFrameworkCore.DependencyInjection;

namespace Template.Common.Jobs;

public static class DependencyInjection
{
    public static IServiceCollection AddBackgroundJobs<TDbContext>(
        this IServiceCollection services,
        string dashboardBasePath = "/admin/tickerq",
        string dashboardPolicyName = "TickerQDashboard") where TDbContext : DbContext
    {
        services.AddTickerQ(options =>
        {
            options.ConfigureScheduler(schedulerOptions =>
            {
                schedulerOptions.MaxConcurrency = 4;
                schedulerOptions.NodeIdentifier = Environment.MachineName;
                schedulerOptions.SchedulerTimeZone = TimeZoneInfo.Utc;
            });

            options.AddOperationalStore(efOptions =>
            {
                efOptions.UseApplicationDbContext<TDbContext>(ConfigurationType.UseModelCustomizer);
                efOptions.SetDbContextPoolSize(16);
            });

            options.AddDashboard(dashboardOptions =>
            {
                dashboardOptions.SetBasePath(dashboardBasePath);
                dashboardOptions.WithHostAuthentication(dashboardPolicyName);
            });
        });

        services.AddScoped<IBackgroundJobScheduler, TickerQBackgroundJobScheduler>();

        return services;
    }
}
