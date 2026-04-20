using System.Security.Cryptography;
using System.Text;
using Grpc.Core;
using Mediator;
using Microsoft.Extensions.Options;
using Template.Application.Products.Queries.GetFacilityProductCount;
using Template.Grpc.Products;
using Template.Infrastructure.Settings;

namespace Template.Products.Api.Services;

public sealed class FacilityProductsGrpcService(
    ISender sender,
    IOptions<InternalServiceAuthSettings> authOptions) : FacilityProductsGrpc.FacilityProductsGrpcBase
{
    private readonly ISender _sender = sender;
    private readonly InternalServiceAuthSettings _authOptions = authOptions.Value;

    public override async Task<FacilityProductUsageReply> GetFacilityProductUsage(
        FacilityProductUsageRequest request,
        ServerCallContext context)
    {
        var providedToken = context.RequestHeaders.GetValue(InternalServiceAuthSettings.HEADER_NAME);
        if (string.IsNullOrEmpty(providedToken) || !IsTokenValid(providedToken))
        {
            throw new RpcException(new Status(StatusCode.PermissionDenied, "Missing or invalid internal service token."));
        }

        if (!Guid.TryParse(request.FacilityId, out var facilityId))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid facility id."));
        }

        var result = await _sender.Send(
            new GetFacilityProductCountQuery(facilityId),
            context.CancellationToken);

        if (result.IsError)
        {
            throw new RpcException(new Status(StatusCode.Internal, result.FirstError.Description));
        }

        return new FacilityProductUsageReply
        {
            FacilityId = request.FacilityId,
            ProductCount = result.Value,
            HasProducts = result.Value > 0
        };
    }

    private bool IsTokenValid(string providedToken)
    {
        var providedBytes = Encoding.UTF8.GetBytes(providedToken);
        var expectedBytes = Encoding.UTF8.GetBytes(_authOptions.Token);
        return CryptographicOperations.FixedTimeEquals(providedBytes, expectedBytes);
    }
}
