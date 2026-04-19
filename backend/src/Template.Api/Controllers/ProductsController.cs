using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Template.Api.Contracts;
using Template.Api.Extensions;
using Template.Api.Products;
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

namespace Template.Api.Controllers;

[Route("api/[controller]")]
[Authorize(Roles = Roles.FACILITY_ADMIN + "," + Roles.FACILITY_EDITOR + "," + Roles.FACILITY_VIEWER)]
public sealed class ProductsController(
    ISender sender,
    IHttpClientFactory httpClientFactory,
    IOptions<ProductsServiceOptions> productsServiceOptions) : ApiController(sender)
{
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
    private readonly ProductsServiceOptions _productsServiceOptions = productsServiceOptions.Value;

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<Product>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        if (_productsServiceOptions.UseProxyMode())
        {
            return await ForwardAsync(HttpMethod.Get, $"api/products?page={page}&pageSize={pageSize}");
        }

        return await SendAsync(new GetProductsQuery(page, pageSize)).ToActionResultAsync();
    }

    [HttpGet("export")]
    [Produces("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
    public async Task<IActionResult> Export()
    {
        if (_productsServiceOptions.UseProxyMode())
        {
            return await ForwardAsync(HttpMethod.Get, "api/products/export");
        }

        return await SendAsync(new ExportProductsToExcelQuery())
            .ToActionResultAsync(file => File(file.Content, file.ContentType, file.FileName));
    }

    [HttpGet("{id:guid}/order-pdf")]
    [Produces("application/pdf")]
    public async Task<IActionResult> ExportOrderPdf(Guid id)
    {
        if (_productsServiceOptions.UseProxyMode())
        {
            return await ForwardAsync(HttpMethod.Get, $"api/products/{id}/order-pdf");
        }

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
        if (_productsServiceOptions.UseProxyMode())
        {
            return await ForwardJsonAsync(HttpMethod.Post, "api/products", command);
        }

        return await SendAsync(command).ToActionResultAsync(id => CreatedAtAction(nameof(GetById), new { id }, id));
    }

    [HttpGet("{id:guid}", Name = "GetProductById")]
    [ProducesResponseType(typeof(Product), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        if (_productsServiceOptions.UseProxyMode())
        {
            return await ForwardAsync(HttpMethod.Get, $"api/products/{id}");
        }

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
        if (_productsServiceOptions.UseProxyMode())
        {
            return await ForwardJsonAsync(HttpMethod.Put, $"api/products/{id}", request);
        }

        return await SendAsync(new UpdateProductCommand(id, request.Name, request.Price))
            .ToActionResultAsync(_ => NoContent());
    }

    [HttpPost("import")]
    [Authorize(Roles = Roles.FACILITY_ADMIN + "," + Roles.FACILITY_EDITOR)]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ProductImportResultDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Import([FromForm] ImportProductsRequest request, CancellationToken cancellationToken)
    {
        if (_productsServiceOptions.UseProxyMode())
        {
            return await ForwardImportAsync(request.File, cancellationToken);
        }

        await using var stream = request.File.OpenReadStream();
        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream, cancellationToken);

        return await SendAsync(new ImportProductsFromExcelCommand(memoryStream.ToArray(), request.File.FileName))
            .ToActionResultAsync();
    }

    private async Task<IActionResult> ForwardJsonAsync<TRequest>(HttpMethod method, string path, TRequest payload)
    {
        using var content = new StringContent(
            JsonSerializer.Serialize(payload),
            Encoding.UTF8,
            "application/json");

        return await ForwardAsync(method, path, content);
    }

    private async Task<IActionResult> ForwardImportAsync(IFormFile file, CancellationToken cancellationToken)
    {
        await using var stream = file.OpenReadStream();
        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream, cancellationToken);

        var fileContent = new ByteArrayContent(memoryStream.ToArray());
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse(file.ContentType);

        using var multipartContent = new MultipartFormDataContent();
        multipartContent.Add(fileContent, nameof(ImportProductsRequest.File), file.FileName);

        return await ForwardAsync(HttpMethod.Post, "api/products/import", multipartContent);
    }

    private async Task<IActionResult> ForwardAsync(HttpMethod method, string path, HttpContent? content = null)
    {
        using var request = new HttpRequestMessage(method, path)
        {
            Content = content
        };

        if (Request.Headers.Authorization.Count > 0)
        {
            request.Headers.TryAddWithoutValidation("Authorization", Request.Headers.Authorization.ToString());
        }

        var client = _httpClientFactory.CreateClient(ProductsServiceOptions.CLIENT_NAME);
        using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, HttpContext.RequestAborted);
        return await ToProxyActionResultAsync(response);
    }

    private async Task<IActionResult> ToProxyActionResultAsync(HttpResponseMessage response)
    {
        var statusCode = (int)response.StatusCode;

        if (response.Content is null)
        {
            return StatusCode(statusCode);
        }

        var contentBytes = await response.Content.ReadAsByteArrayAsync(HttpContext.RequestAborted);
        if (contentBytes.Length == 0)
        {
            return StatusCode(statusCode);
        }

        var contentType = response.Content.Headers.ContentType?.ToString() ?? "application/octet-stream";
        var fileName = response.Content.Headers.ContentDisposition?.FileNameStar
            ?? response.Content.Headers.ContentDisposition?.FileName;

        if (!string.IsNullOrWhiteSpace(fileName) || IsBinaryContentType(contentType))
        {
            return File(contentBytes, contentType, fileName?.Trim('"'));
        }

        return new ContentResult
        {
            StatusCode = statusCode,
            ContentType = contentType,
            Content = Encoding.UTF8.GetString(contentBytes)
        };
    }

    private static bool IsBinaryContentType(string contentType)
    {
        return contentType.StartsWith("application/pdf", StringComparison.OrdinalIgnoreCase) ||
               contentType.StartsWith("application/vnd.openxmlformats-officedocument", StringComparison.OrdinalIgnoreCase) ||
               contentType.StartsWith("application/octet-stream", StringComparison.OrdinalIgnoreCase);
    }
}
