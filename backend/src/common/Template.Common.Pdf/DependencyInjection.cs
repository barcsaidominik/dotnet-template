using Microsoft.Extensions.DependencyInjection;
using QuestPDF.Infrastructure;

namespace Template.Common.Pdf;

public static class DependencyInjection
{
    public static IServiceCollection AddQuestPdf(this IServiceCollection services)
    {
        QuestPDF.Settings.License = LicenseType.Community;
        return services;
    }
}
