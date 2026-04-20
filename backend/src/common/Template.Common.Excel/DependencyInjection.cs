using Microsoft.Extensions.DependencyInjection;
using Template.Application.Common.Interfaces;

namespace Template.Common.Excel;

public static class DependencyInjection
{
    public static IServiceCollection AddExcel(this IServiceCollection services)
    {
        services.AddScoped<IExcelWorkbookService, ClosedXmlExcelWorkbookService>();
        return services;
    }
}
