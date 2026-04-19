using ErrorOr;
using Mediator;
using Template.Application.Common.Dtos;
using Template.Application.Common.Interfaces;

namespace Template.Application.Admin.Queries.ExportUsersToExcel;

public sealed class ExportUsersToExcelQueryHandler(
    IAuthService authService,
    IExcelWorkbookService excelWorkbookService)
    : IRequestHandler<ExportUsersToExcelQuery, ErrorOr<ExcelFileDto>>
{
    private const string CONTENT_TYPE = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    private readonly IAuthService _authService = authService;
    private readonly IExcelWorkbookService _excelWorkbookService = excelWorkbookService;

    public async ValueTask<ErrorOr<ExcelFileDto>> Handle(ExportUsersToExcelQuery request, CancellationToken ct)
    {
        var usersResult = await _authService.GetAllUsersAsync(ct);
        if (usersResult.IsError)
        {
            return usersResult.Errors;
        }

        var rows = usersResult.Value
            .OrderBy(user => user.Email)
            .Select(user => new UserExcelExportRowDto(
                user.Id,
                user.Email,
                user.Role ?? "-",
                user.IsApproved ? "Approved" : "Pending",
                user.FacilityId?.ToString() ?? "-",
                "-"))
            .ToList();

        var content = _excelWorkbookService.ExportUsers(rows);
        var fileName = $"users-{DateTime.UtcNow:yyyyMMdd-HHmmss}.xlsx";

        return new ExcelFileDto(fileName, CONTENT_TYPE, content);
    }
}
