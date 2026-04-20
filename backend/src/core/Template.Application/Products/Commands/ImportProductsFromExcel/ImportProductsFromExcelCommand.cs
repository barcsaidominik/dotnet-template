using ErrorOr;
using Mediator;
using Template.Application.Common.Dtos;

namespace Template.Application.Products.Commands.ImportProductsFromExcel;

public sealed record ImportProductsFromExcelCommand(byte[] Content, string FileName) : IRequest<ErrorOr<ProductImportResultDto>>;
