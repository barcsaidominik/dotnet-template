using System.Globalization;
using ErrorOr;
using Mediator;
using Template.Application.Common.Dtos;
using Template.Application.Common.Interfaces;

namespace Template.Application.Admin.Queries.ExportUsersToExcel;

public sealed class ExportUsersToExcelQueryHandler(
    IAuthService authService,
    IExcelWorkbookService excelWorkbookService,
    ICurrentUserService currentUserService)
    : IRequestHandler<ExportUsersToExcelQuery, ErrorOr<ExcelFileDto>>
{
    private const string CONTENT_TYPE = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    private readonly IAuthService _authService = authService;
    private readonly IExcelWorkbookService _excelWorkbookService = excelWorkbookService;
    private readonly ICurrentUserService _currentUserService = currentUserService;

    public async ValueTask<ErrorOr<ExcelFileDto>> Handle(ExportUsersToExcelQuery request, CancellationToken ct)
    {
        var usersResult = await _authService.GetAllUsersAsync(ct);
        if (usersResult.IsError)
        {
            return usersResult.Errors;
        }

        var filtered = usersResult.Value.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var searchLower = request.Search.ToLowerInvariant();
            filtered = filtered.Where(u => u.Email.ToLowerInvariant().Contains(searchLower));
        }

        filtered = ApplySorting(filtered, request.SortBy, request.SortDescending);

        var rows = filtered
            .Select(user => new UserExcelExportRowDto(
                user.Id,
                user.Email,
                user.Role ?? "-",
                user.IsApproved ? "Approved" : "Pending",
                user.FacilityId?.ToString() ?? "-",
                "-"))
            .ToList();

        var culture = await ResolveCultureAsync(ct);
        var content = _excelWorkbookService.ExportUsers(rows, culture);
        var fileName = $"users-{DateTime.UtcNow:yyyyMMdd-HHmmss}.xlsx";

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

    private static IEnumerable<UserDto> ApplySorting(IEnumerable<UserDto> users, string? sortBy, bool descending)
    {
        return sortBy?.ToLowerInvariant() switch
        {
            "role" => descending ? users.OrderByDescending(u => u.Role) : users.OrderBy(u => u.Role),
            "email" => descending ? users.OrderByDescending(u => u.Email) : users.OrderBy(u => u.Email),
            _ => users.OrderBy(u => u.Email)
        };
    }
}
