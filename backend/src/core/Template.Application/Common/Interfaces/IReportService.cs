namespace Template.Application.Common.Interfaces;

public interface IReportService
{
    Task<byte[]> GetReportAsync(string reportName, CancellationToken ct = default);
}
