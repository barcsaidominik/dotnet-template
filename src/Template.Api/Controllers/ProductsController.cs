using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Template.Api.Extensions;
using Template.Application.Products.Commands.CreateProduct;
using Template.Application.Products.Queries.GetProductById;
using Template.Domain.Constants;
using Template.Domain.Entities;

namespace Template.Api.Controllers;

[Route("api/[controller]")]
[Authorize(Roles = Roles.FacilityAdmin + "," + Roles.FacilityEditor + "," + Roles.FacilityViewer)]
public sealed class ProductsController(ISender sender) : ApiController(sender)
{
    [HttpPost]
    [Authorize(Roles = Roles.FacilityAdmin + "," + Roles.FacilityEditor)]
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
}
