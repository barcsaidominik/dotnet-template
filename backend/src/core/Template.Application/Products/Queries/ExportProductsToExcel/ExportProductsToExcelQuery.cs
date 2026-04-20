using ErrorOr;
using Mediator;
using Template.Application.Common.Dtos;

namespace Template.Application.Products.Queries.ExportProductsToExcel;

public sealed record ExportProductsToExcelQuery(
    string? Search = null,
    string? SortBy = null,
    bool SortDescending = false) : IRequest<ErrorOr<ExcelFileDto>>;
