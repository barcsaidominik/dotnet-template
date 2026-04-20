using System.Globalization;
using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Template.Application.Common.Dtos;
using Template.Application.Common.Interfaces;
using Template.Domain.Entities;

namespace Template.Application.Admin.Queries.ExportFacilitiesToExcel;

public sealed class ExportFacilitiesToExcelQueryHandler(
    IEntityStore<Facility> store,
    IExcelWorkbookService excelWorkbookService,
    ICurrentUserService currentUserService,
    IAuthService authService)
    : IRequestHandler<ExportFacilitiesToExcelQuery, ErrorOr<ExcelFileDto>>
{
    private const string CONTENT_TYPE = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    private readonly IEntityStore<Facility> _store = store;
    private readonly IExcelWorkbookService _excelWorkbookService = excelWorkbookService;
    private readonly ICurrentUserService _currentUserService = currentUserService;
    private readonly IAuthService _authService = authService;

    public async ValueTask<ErrorOr<ExcelFileDto>> Handle(ExportFacilitiesToExcelQuery request, CancellationToken ct)
    {
        var facilities = await _store.GetQuery(asNoTracking: true, skipGuards: true)
            .OrderBy(facility => facility.Name)
            .Select(facility => new FacilityExcelExportRowDto(facility.Id, facility.Name))
            .ToListAsync(ct);

        var culture = await ResolveCultureAsync(ct);
        var content = _excelWorkbookService.ExportFacilities(facilities, culture);
        var fileName = $"facilities-{DateTime.UtcNow:yyyyMMdd-HHmmss}.xlsx";

        return new ExcelFileDto(fileName, CONTENT_TYPE, content);
    }

    private async Task<CultureInfo?> ResolveCultureAsync(CancellationToken ct)
    {
        if (!_currentUserService.IsAuthenticated || _currentUserService.UserId == Guid.Empty)
        {
            return null;
        }

        var preferredLanguage = await _authService.GetUserPreferredLanguageAsync(_currentUserService.UserId, ct);
        if (preferredLanguage is null)
        {
            return null;
        }

        return CultureInfo.GetCultureInfo(preferredLanguage);
    }
}
