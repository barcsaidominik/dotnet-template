using System.Globalization;
using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Template.Application.Common.Dtos;
using Template.Application.Common.Interfaces;
using Template.Domain.Entities;

namespace Template.Application.Products.Queries.ExportProductsToExcel;

public sealed class ExportProductsToExcelQueryHandler(
    IEntityStore<Product> store,
    IExcelWorkbookService excelWorkbookService,
    ICurrentUserService currentUserService,
    IAuthService authService)
    : IRequestHandler<ExportProductsToExcelQuery, ErrorOr<ExcelFileDto>>
{
    private const string CONTENT_TYPE = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    private readonly IEntityStore<Product> _store = store;
    private readonly IExcelWorkbookService _excelWorkbookService = excelWorkbookService;
    private readonly ICurrentUserService _currentUserService = currentUserService;
    private readonly IAuthService _authService = authService;

    public async ValueTask<ErrorOr<ExcelFileDto>> Handle(ExportProductsToExcelQuery request, CancellationToken ct)
    {
        var products = await _store.GetQuery(asNoTracking: true)
            .OrderBy(product => product.Name)
            .Select(product => new ProductExcelExportRowDto(
                product.Id,
                product.Name,
                product.Price,
                product.Quantity,
                product.FacilityId,
                product.CreatedAt))
            .ToListAsync(ct);

        var culture = await ResolveCultureAsync(ct);
        var content = _excelWorkbookService.ExportProducts(products, culture);
        var fileName = $"products-{DateTime.UtcNow:yyyyMMdd-HHmmss}.xlsx";

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
