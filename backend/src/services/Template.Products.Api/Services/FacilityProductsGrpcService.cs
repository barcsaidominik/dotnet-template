using Grpc.Core;
using Mediator;
using Microsoft.AspNetCore.Authorization;
using Template.Application.Products.Queries.GetFacilityProductCount;
using Template.Grpc.Products;

namespace Template.Products.Api.Services;

[Authorize]
public sealed class FacilityProductsGrpcService(ISender sender) : FacilityProductsGrpc.FacilityProductsGrpcBase
{
    private readonly ISender _sender = sender;

    public override async Task<FacilityProductUsageReply> GetFacilityProductUsage(
        FacilityProductUsageRequest request,
        ServerCallContext context)
    {
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
}
