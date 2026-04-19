using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using Template.Grpc.Products;
using Template.Infrastructure.Persistence;

namespace Template.Products.Api.Services;

public sealed class FacilityProductsGrpcService(AppDbContext dbContext) : FacilityProductsGrpc.FacilityProductsGrpcBase
{
    private readonly AppDbContext _dbContext = dbContext;

    public override async Task<FacilityProductUsageReply> GetFacilityProductUsage(FacilityProductUsageRequest request, ServerCallContext context)
    {
        if (!Guid.TryParse(request.FacilityId, out var facilityId))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid facility id."));
        }

        var productCount = await _dbContext.Products
            .AsNoTracking()
            .CountAsync(product => product.FacilityId == facilityId, context.CancellationToken);

        return new FacilityProductUsageReply
        {
            FacilityId = request.FacilityId,
            ProductCount = productCount,
            HasProducts = productCount > 0
        };
    }
}
