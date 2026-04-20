using Grpc.Core;
using Microsoft.Extensions.Options;
using Template.Application.Common.Interfaces;
using Template.Common.Retry;
using Template.Grpc.Products;
using Template.Infrastructure.Settings;

namespace Template.Infrastructure.Products;

public sealed class GrpcFacilityProductUsageService(
    FacilityProductsGrpc.FacilityProductsGrpcClient client,
    IOptions<InternalServiceAuthSettings> authOptions) : IFacilityProductUsageService
{
    private readonly FacilityProductsGrpc.FacilityProductsGrpcClient _client = client;
    private readonly InternalServiceAuthSettings _authOptions = authOptions.Value;

    public async Task<int> GetProductCountAsync(Guid facilityId, CancellationToken ct = default)
    {
        var metadata = new Metadata
        {
            { InternalServiceAuthSettings.HEADER_NAME, _authOptions.Token }
        };

        var request = new FacilityProductUsageRequest
        {
            FacilityId = facilityId.ToString()
        };

        var reply = await RetryHelpers.RetryAsync(
            cancellationToken => _client.GetFacilityProductUsageAsync(request, headers: metadata, cancellationToken: cancellationToken).ResponseAsync,
            shouldNotRetry: ex => ex is RpcException rpc &&
                rpc.StatusCode is StatusCode.PermissionDenied
                    or StatusCode.InvalidArgument
                    or StatusCode.NotFound
                    or StatusCode.Unauthenticated,
            retryCount: 3,
            retryDelayInSeconds: 1,
            isExponentialWait: true,
            cancellationToken: ct
        );

        return reply.ProductCount;
    }
}
