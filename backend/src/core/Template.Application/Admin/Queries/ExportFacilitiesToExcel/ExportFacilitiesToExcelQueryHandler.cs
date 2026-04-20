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
        var query = _store.GetQuery(asNoTracking: true, skipGuards: true);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var searchLower = request.Search.ToLowerInvariant();
            query = query.Where(f => f.Name.ToLower().Contains(searchLower));
        }

        query = ApplySorting(query, request.SortBy, request.SortDescending);

        var facilities = await query
            .Take(10_000)
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
        return GetCultureSafe(preferredLanguage);
    }

    private static CultureInfo? GetCultureSafe(string? preferredLanguage)
    {
        if (preferredLanguage is null)
        {
            return null;
        }

        try
        {
            return CultureInfo.GetCultureInfo(preferredLanguage);
        }
        catch (CultureNotFoundException)
        {
            return CultureInfo.GetCultureInfo("hu-HU");
        }
    }

    private static IQueryable<Facility> ApplySorting(IQueryable<Facility> query, string? sortBy, bool descending)
    {
        return sortBy?.ToLowerInvariant() switch
        {
            "name" => descending ? query.OrderByDescending(f => f.Name) : query.OrderBy(f => f.Name),
            _ => query.OrderBy(f => f.Name)
        };
    }
}
