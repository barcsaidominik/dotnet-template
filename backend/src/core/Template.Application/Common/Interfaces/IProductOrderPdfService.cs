using Template.Application.Common.Dtos;

namespace Template.Application.Common.Interfaces;

public interface IProductOrderPdfService
{
    Task<PdfFileDto> GenerateAsync(ProductOrderPdfModel model, CancellationToken ct = default);
}
