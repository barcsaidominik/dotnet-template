using Mediator;
using Template.Api.Extensions;
using Template.Application.Products.Commands.CreateProduct;
using Template.Application.Products.Queries.GetProductById;

namespace Template.Api.Endpoints;

public static class ProductEndpoints
{
    public static IEndpointRouteBuilder MapProductEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/products");

        group.MapPost("/", CreateProduct)
            .WithName("CreateProduct");

        group.MapGet("/{id:guid}", GetProductById)
            .WithName("GetProductById");

        return app;
    }

    private static async Task<IResult> CreateProduct(
        CreateProductRequest request,
        ISender sender,
        CancellationToken ct)
    {
        var command = new CreateProductCommand(request.Name, request.Price);
        var result = await sender.Send(command, ct);

        return result.ToCreatedResult("GetProductById", new { id = result.Value });
    }

    private static async Task<IResult> GetProductById(
        Guid id,
        ISender sender,
        CancellationToken ct)
    {
        var query = new GetProductByIdQuery(id);
        var result = await sender.Send(query, ct);

        return result.ToApiResult();
    }
}

public sealed record CreateProductRequest(string Name, decimal Price);
