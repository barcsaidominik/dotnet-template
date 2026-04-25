using Template.Application.Common.Interfaces;

namespace Template.Infrastructure.Reports;

public sealed class ReportService : IReportService
{
    private readonly string _basePath = "/app/reports";

    public async Task<byte[]> GetReportAsync(string reportName, CancellationToken ct = default)
    {
        var filePath = Path.Combine(_basePath, reportName);
        return await File.ReadAllBytesAsync(filePath, ct);
    }
}
