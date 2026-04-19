using Template.Application.Common.Interfaces;
using Template.Grpc.Products;

namespace Template.Infrastructure.Products;

public sealed class GrpcFacilityProductUsageService(FacilityProductsGrpc.FacilityProductsGrpcClient client) : IFacilityProductUsageService
{
    private readonly FacilityProductsGrpc.FacilityProductsGrpcClient _client = client;

    public async Task<int> GetProductCountAsync(Guid facilityId, CancellationToken ct = default)
    {
        var reply = await _client.GetFacilityProductUsageAsync(
            new FacilityProductUsageRequest
            {
                FacilityId = facilityId.ToString()
            },
            cancellationToken: ct);

        return reply.ProductCount;
    }
}
