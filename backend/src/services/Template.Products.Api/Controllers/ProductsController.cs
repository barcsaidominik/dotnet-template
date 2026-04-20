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
using Template.Common.Extensions;
using Template.Domain.Constants;
using Template.Domain.Entities;
using Template.Products.Api.Contracts;

namespace Template.Products.Api.Controllers;

[Route("api/[controller]")]
[Authorize(Roles = Roles.FACILITY_ADMIN + "," + Roles.FACILITY_EDITOR + "," + Roles.FACILITY_VIEWER)]
public sealed class ProductsController(ISender sender)
    : Template.Common.Controllers.ApiController(sender)
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<Product>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] bool sortDescending = false)
    {
        return await SendAsync(new GetProductsQuery(page, pageSize, search, sortBy, sortDescending)).ToActionResultAsync();
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
        return await SendAsync(new UpdateProductCommand(id, request.Name, request.Price, request.Quantity))
            .ToActionResultAsync(_ => NoContent());
    }

    [HttpPost("import")]
    [Authorize(Roles = Roles.FACILITY_ADMIN + "," + Roles.FACILITY_EDITOR)]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(10_485_760)]
    [RequestFormLimits(MultipartBodyLengthLimit = 10_485_760)]
    [ProducesResponseType(typeof(ProductImportResultDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Import([FromForm] ImportProductsRequest request, CancellationToken cancellationToken)
    {
        if (!request.File.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest("Only .xlsx files are supported.");
        }

        await using var stream = request.File.OpenReadStream();
        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream, cancellationToken);

        return await SendAsync(new ImportProductsFromExcelCommand(memoryStream.ToArray(), request.File.FileName))
            .ToActionResultAsync();
    }
}
