using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using ClosedXML.Excel;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Template.Application.Common.Dtos;
using Template.Application.Products.Commands.CreateProduct;
using Template.Domain.Entities;

namespace Template.Tests;

[Collection("Products integration")]
public class ProductsIntegrationTests(ProductsIntegrationTestWebApplicationFactory factory)
{
    private readonly ProductsIntegrationTestWebApplicationFactory _factory = factory;

    private sealed record ProductDto(Guid Id, string Name, decimal Price, Guid FacilityId);
    private sealed record PagedProductResult(IReadOnlyList<ProductDto> Items, int TotalCount, int Page, int PageSize);

    [Fact]
    public async Task GetAll_WithAuthenticatedFacilityAdmin_ReturnsPagedProducts()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await _factory.ResetStateAsync(ct);
        using var client = _factory.CreateFacilityAdminClient();

        // Act
        var response = await client.GetAsync("/api/products?page=1&pageSize=10", ct);
        var result = await response.Content.ReadFromJsonAsync<PagedProductResult>(cancellationToken: ct);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Should().NotBeNull();
        result!.Items.Should().ContainSingle();
        result.Items[0].Name.Should().Be("Seeded Product");
        result.TotalCount.Should().Be(1);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(10);
    }

    [Fact]
    public async Task GetAll_WithPagination_ReturnsCorrectPage()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await _factory.ResetStateAsync(ct);

        // Add more products for pagination testing
        await _factory.ExecuteDbContextAsync(async dbContext =>
        {
            for (var i = 1; i <= 5; i++)
            {
                var product = Product.Create($"Product {i}", 10m * i, _factory.FacilityId).Value;
                dbContext.Products.Add(product);
            }
            await dbContext.SaveChangesAsync(ct);
            return true;
        });

        using var client = _factory.CreateFacilityAdminClient();

        // Act
        var response = await client.GetAsync("/api/products?page=2&pageSize=3", ct);
        var result = await response.Content.ReadFromJsonAsync<PagedProductResult>(cancellationToken: ct);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Should().NotBeNull();
        result!.TotalCount.Should().Be(6); // 1 seeded + 5 added
        result.Page.Should().Be(2);
        result.PageSize.Should().Be(3);
        result.Items.Count.Should().Be(3); // Second page with 3 items
    }

    [Fact]
    public async Task GetById_WithValidId_ReturnsProduct()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await _factory.ResetStateAsync(ct);
        using var client = _factory.CreateFacilityAdminClient();

        // Act
        var response = await client.GetAsync($"/api/products/{_factory.SeededProductId}", ct);
        var result = await response.Content.ReadFromJsonAsync<ProductDto>(cancellationToken: ct);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Should().NotBeNull();
        result!.Name.Should().Be("Seeded Product");
        result.Price.Should().Be(55.25m);
    }

    [Fact]
    public async Task GetById_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await _factory.ResetStateAsync(ct);
        using var client = _factory.CreateFacilityAdminClient();
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await client.GetAsync($"/api/products/{nonExistentId}", ct);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Create_WithValidData_CreatesProduct()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await _factory.ResetStateAsync(ct);
        using var client = _factory.CreateFacilityAdminClient();

        var command = new CreateProductCommand("New Product", 123.45m);

        // Act
        var response = await client.PostAsJsonAsync("/api/products", command, ct);
        var newProductId = await response.Content.ReadFromJsonAsync<Guid>(cancellationToken: ct);
        var createdProduct = await _factory.ExecuteDbContextAsync(
            dbContext => dbContext.Products.FirstOrDefaultAsync(p => p.Id == newProductId, ct));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        newProductId.Should().NotBeEmpty();
        createdProduct.Should().NotBeNull();
        createdProduct!.Name.Should().Be("New Product");
        createdProduct.Price.Should().Be(123.45m);
        createdProduct.FacilityId.Should().Be(_factory.FacilityId);
    }

    [Fact]
    public async Task Create_WithInvalidData_ReturnsBadRequest()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await _factory.ResetStateAsync(ct);
        using var client = _factory.CreateFacilityAdminClient();

        // Empty name should fail validation
        var command = new CreateProductCommand("", 123.45m);

        // Act
        var response = await client.PostAsJsonAsync("/api/products", command, ct);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Update_WithValidData_UpdatesProduct()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await _factory.ResetStateAsync(ct);
        using var client = _factory.CreateFacilityAdminClient();

        // Act
        var response = await client.PutAsJsonAsync(
            $"/api/products/{_factory.SeededProductId}",
            new
            {
                Name = "Updated Product",
                Price = 99.99m
            },
            ct);

        var updatedProduct = await _factory.ExecuteDbContextAsync(
            dbContext => dbContext.Products.FirstOrDefaultAsync(p => p.Id == _factory.SeededProductId, ct));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        updatedProduct.Should().NotBeNull();
        updatedProduct!.Name.Should().Be("Updated Product");
        updatedProduct.Price.Should().Be(99.99m);
    }

    [Fact]
    public async Task Update_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await _factory.ResetStateAsync(ct);
        using var client = _factory.CreateFacilityAdminClient();
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await client.PutAsJsonAsync(
            $"/api/products/{nonExistentId}",
            new
            {
                Name = "Updated Product",
                Price = 99.99m
            },
            ct);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Export_WithAuthenticatedFacilityAdmin_ReturnsExcelWorkbook()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await _factory.ResetStateAsync(ct);
        using var client = _factory.CreateFacilityAdminClient();

        // Act
        var response = await client.GetAsync("/api/products/export", ct);
        var content = await response.Content.ReadAsByteArrayAsync(ct);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");

        using var workbook = new XLWorkbook(new MemoryStream(content));
        var worksheet = workbook.Worksheet("Products");
        worksheet.Cell(2, 2).GetString().Should().Be("Seeded Product");
        worksheet.Cell(2, 3).GetValue<decimal>().Should().Be(55.25m);
    }

    [Fact]
    public async Task Import_WithAuthenticatedFacilityAdmin_PersistsProducts()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await _factory.ResetStateAsync(ct);
        using var client = _factory.CreateFacilityAdminClient();

        using var workbook = new XLWorkbook();
        var worksheet = workbook.AddWorksheet("Products");
        worksheet.Cell(1, 1).Value = "Name";
        worksheet.Cell(1, 2).Value = "Price";
        worksheet.Cell(2, 1).Value = "Imported Product";
        worksheet.Cell(2, 2).Value = 99.90m;

        byte[] excelContent;
        using (var stream = new MemoryStream())
        {
            workbook.SaveAs(stream);
            excelContent = stream.ToArray();
        }

        using var multipart = new MultipartFormDataContent();
        using var fileContent = new ByteArrayContent(excelContent);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        multipart.Add(fileContent, "File", "products.xlsx");

        // Act
        var response = await client.PostAsync("/api/products/import", multipart, ct);
        var payload = await response.Content.ReadFromJsonAsync<ProductImportResultDto>(cancellationToken: ct);
        var importedProducts = await _factory.ExecuteDbContextAsync(dbContext =>
            Task.FromResult(dbContext.Products.Where(product => product.Name == "Imported Product").ToList()));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        payload.Should().NotBeNull();
        payload!.ImportedCount.Should().Be(1);
        payload.SkippedCount.Should().Be(0);
        importedProducts.Should().ContainSingle();
        importedProducts[0].FacilityId.Should().Be(_factory.FacilityId);
    }

    [Fact]
    public async Task ExportOrderPdf_WithAuthenticatedFacilityAdmin_ReturnsPdfFile()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await _factory.ResetStateAsync(ct);
        using var client = _factory.CreateFacilityAdminClient();

        // Act
        var response = await client.GetAsync($"/api/products/{_factory.SeededProductId}/order-pdf", ct);
        var content = await response.Content.ReadAsByteArrayAsync(ct);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/pdf");
        response.Content.Headers.ContentDisposition?.FileName.Should().Contain("Seeded Product-order-template.pdf");
        content.Take(4).Should().Equal("%PDF"u8.ToArray());
    }
}
