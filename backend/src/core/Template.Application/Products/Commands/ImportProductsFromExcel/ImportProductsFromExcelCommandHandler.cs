using ErrorOr;
using Mediator;
using Template.Application.Common.Dtos;
using Template.Application.Common.Interfaces;
using Template.Domain.Entities;

namespace Template.Application.Products.Commands.ImportProductsFromExcel;

public sealed class ImportProductsFromExcelCommandHandler(
    IEntityStore<Product> store,
    ICurrentUserService currentUserService,
    IExcelWorkbookService excelWorkbookService)
    : IRequestHandler<ImportProductsFromExcelCommand, ErrorOr<ProductImportResultDto>>
{
    private readonly IEntityStore<Product> _store = store;
    private readonly ICurrentUserService _currentUserService = currentUserService;
    private readonly IExcelWorkbookService _excelWorkbookService = excelWorkbookService;

    public async ValueTask<ErrorOr<ProductImportResultDto>> Handle(ImportProductsFromExcelCommand request, CancellationToken ct)
    {
        if (!_currentUserService.FacilityId.HasValue)
        {
            return Error.Forbidden("ProductImport.NoFacility", "User is not assigned to a facility.");
        }

        if (request.Content.Length == 0)
        {
            return Error.Validation("ProductImport.EmptyFile", "The uploaded file is empty.");
        }

        var importRowsResult = _excelWorkbookService.ImportProducts(request.Content);
        if (importRowsResult.IsError)
        {
            return importRowsResult.Errors;
        }

        var errors = new List<ExcelImportErrorDto>();
        var importedCount = 0;

        foreach (var row in importRowsResult.Value)
        {
            var productResult = Product.Create(row.Name, row.Price, _currentUserService.FacilityId.Value, row.Quantity);
            if (productResult.IsError)
            {
                errors.AddRange(productResult.Errors.Select(error => new ExcelImportErrorDto(row.RowNumber, error.Description)));
                continue;
            }

            await _store.AddAsync(productResult.Value, ct);
            importedCount++;
        }

        if (importedCount > 0)
        {
            await _store.SaveChangesAsync(ct);
        }

        return new ProductImportResultDto(importedCount, errors.Count, errors.AsReadOnly());
    }
}
