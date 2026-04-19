using ErrorOr;
using Mediator;
using Template.Application.Common.Dtos;

namespace Template.Application.Products.Queries.ExportProductOrderPdf;

public sealed record ExportProductOrderPdfQuery(Guid ProductId) : IRequest<ErrorOr<PdfFileDto>>;
