using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using ClosedXML.Excel;
using FluentAssertions;
using Template.Application.Common.Dtos;

namespace Template.Tests;

[Collection("Products integration")]
public class ProductsIntegrationTests(ProductsIntegrationTestWebApplicationFactory factory)
{
    private readonly ProductsIntegrationTestWebApplicationFactory _factory = factory;

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
