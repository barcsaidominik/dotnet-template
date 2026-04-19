using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Template.Application.Common.Dtos;
using Template.Application.Products.Commands.CreateProduct;
using Template.Application.Products.Commands.ImportProductsFromExcel;
using Template.Application.Products.Commands.UpdateProduct;
using Template.Application.Products.Queries.ExportProductOrderPdf;
using Template.Application.Products.Queries.ExportProductsToExcel;
using Template.Application.Products.Queries.GetProductById;
using Template.Application.Products.Queries.GetProducts;
using Template.Domain.Constants;
using Template.Domain.Entities;
using Template.Products.Api.Contracts;
using Template.Products.Api.Extensions;

namespace Template.Products.Api.Controllers;

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

    [HttpGet("export")]
    [Produces("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
    public async Task<IActionResult> Export()
    {
        return await SendAsync(new ExportProductsToExcelQuery())
            .ToActionResultAsync(file => File(file.Content, file.ContentType, file.FileName));
    }

    [HttpGet("{id:guid}/order-pdf")]
    [Produces("application/pdf")]
    public async Task<IActionResult> ExportOrderPdf(Guid id)
    {
        return await SendAsync(new ExportProductOrderPdfQuery(id))
            .ToActionResultAsync(file => File(file.Content, file.ContentType, file.FileName));
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

    [HttpPost("import")]
    [Authorize(Roles = Roles.FACILITY_ADMIN + "," + Roles.FACILITY_EDITOR)]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ProductImportResultDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Import([FromForm] ImportProductsRequest request, CancellationToken cancellationToken)
    {
        await using var stream = request.File.OpenReadStream();
        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream, cancellationToken);

        return await SendAsync(new ImportProductsFromExcelCommand(memoryStream.ToArray(), request.File.FileName))
            .ToActionResultAsync();
    }
}
