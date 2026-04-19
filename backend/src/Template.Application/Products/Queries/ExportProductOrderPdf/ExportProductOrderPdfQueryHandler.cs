using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Template.Application.Common.Dtos;
using Template.Application.Common.Interfaces;
using Template.Domain.Entities;
using Template.Domain.Errors;

namespace Template.Application.Products.Queries.ExportProductOrderPdf;

public sealed class ExportProductOrderPdfQueryHandler(
    IEntityStore<Product> productStore,
    IEntityStore<Facility> facilityStore,
    ICurrentUserService currentUserService,
    IAuthService authService,
    IProductOrderPdfService productOrderPdfService) : IRequestHandler<ExportProductOrderPdfQuery, ErrorOr<PdfFileDto>>
{
    private readonly IEntityStore<Product> _productStore = productStore;
    private readonly IEntityStore<Facility> _facilityStore = facilityStore;
    private readonly ICurrentUserService _currentUserService = currentUserService;
    private readonly IAuthService _authService = authService;
    private readonly IProductOrderPdfService _productOrderPdfService = productOrderPdfService;

    public async ValueTask<ErrorOr<PdfFileDto>> Handle(ExportProductOrderPdfQuery request, CancellationToken ct)
    {
        var product = await _productStore.GetQuery(asNoTracking: true)
            .FirstOrDefaultAsync(x => x.Id == request.ProductId, ct);

        if (product is null)
        {
            return ProductErrors.NotFound;
        }

        var facilityName = await _facilityStore.GetQuery(asNoTracking: true, skipGuards: true)
            .Where(x => x.Id == product.FacilityId)
            .Select(x => x.Name)
            .FirstOrDefaultAsync(ct);

        var userResult = await _authService.GetUserByIdAsync(_currentUserService.UserId, ct);
        var requestedByEmail = userResult.IsError ? "unknown@template.local" : userResult.Value.Email;

        var model = new ProductOrderPdfModel(
            product.Id,
            product.Name,
            product.Price,
            product.FacilityId,
            facilityName ?? product.FacilityId.ToString(),
            requestedByEmail,
            DateTime.UtcNow);

        return await _productOrderPdfService.GenerateAsync(model, ct);
    }
}
