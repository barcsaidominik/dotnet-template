using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Template.Application.Common.Dtos;
using Template.Application.Common.Interfaces;
using Template.Domain.Entities;

namespace Template.Application.Products.Queries.ExportProductsToExcel;

public sealed class ExportProductsToExcelQueryHandler(
    IEntityStore<Product> store,
    IExcelWorkbookService excelWorkbookService)
    : IRequestHandler<ExportProductsToExcelQuery, ErrorOr<ExcelFileDto>>
{
    private const string CONTENT_TYPE = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    private readonly IEntityStore<Product> _store = store;
    private readonly IExcelWorkbookService _excelWorkbookService = excelWorkbookService;

    public async ValueTask<ErrorOr<ExcelFileDto>> Handle(ExportProductsToExcelQuery request, CancellationToken ct)
    {
        var products = await _store.GetQuery(asNoTracking: true)
            .OrderBy(product => product.Name)
            .Select(product => new ProductExcelExportRowDto(
                product.Id,
                product.Name,
                product.Price,
                product.FacilityId,
                product.CreatedAt))
            .ToListAsync(ct);

        var content = _excelWorkbookService.ExportProducts(products);
        var fileName = $"products-{DateTime.UtcNow:yyyyMMdd-HHmmss}.xlsx";

        return new ExcelFileDto(fileName, CONTENT_TYPE, content);
    }
}
