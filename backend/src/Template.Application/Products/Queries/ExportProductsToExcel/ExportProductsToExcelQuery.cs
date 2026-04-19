using ErrorOr;
using Mediator;
using Template.Application.Common.Dtos;

namespace Template.Application.Products.Queries.ExportProductsToExcel;

public sealed record ExportProductsToExcelQuery : IRequest<ErrorOr<ExcelFileDto>>;
