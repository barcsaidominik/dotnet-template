using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Template.Api.Contracts;
using Template.Api.Extensions;
using Template.Application.Common.Dtos;
using Template.Application.Products.Commands.CreateProduct;
using Template.Application.Products.Commands.UpdateProduct;
using Template.Application.Products.Queries.GetProductById;
using Template.Application.Products.Queries.GetProducts;
using Template.Domain.Constants;
using Template.Domain.Entities;

namespace Template.Api.Controllers;

[Route("api/[controller]")]
[Authorize(Roles = Roles.FACILITY_ADMIN + "," + Roles.FACILITY_EDITOR + "," + Roles.FACILITY_VIEWER)]
public sealed class ProductsController(ISender sender) : ApiController(sender)
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<Product>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        return await SendAsync(new GetProductsQuery(page, pageSize)).ToActionResultAsync();
    }

    [HttpPost]
    [Authorize(Roles = Roles.FACILITY_ADMIN + "," + Roles.FACILITY_EDITOR)]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create([FromBody] CreateProductCommand command)
    {
        return await SendAsync(command).ToActionResultAsync(id => CreatedAtAction(nameof(GetById), new { id }, id));
    }

    [HttpGet("{id:guid}", Name = "GetProductById")]
    [ProducesResponseType(typeof(Product), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        return await SendAsync(new GetProductByIdQuery(id)).ToActionResultAsync();
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = Roles.FACILITY_ADMIN + "," + Roles.FACILITY_EDITOR)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateProductRequest request)
    {
        return await SendAsync(new UpdateProductCommand(id, request.Name, request.Price))
            .ToActionResultAsync(_ => NoContent());
    }
}
