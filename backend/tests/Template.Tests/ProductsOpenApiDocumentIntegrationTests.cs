using System.Net;
using System.Text.Json;
using FluentAssertions;

namespace Template.Tests;

/// <summary>
/// Products API counterpart of <see cref="OpenApiDocumentIntegrationTests"/>. Template.Products.Api
/// generates its own document and registers the same BearerSecuritySchemeTransformer, so it needs
/// its own coverage of the pinned Microsoft.OpenApi dependency.
/// </summary>
[Collection("Products integration")]
public class ProductsOpenApiDocumentIntegrationTests(ProductsIntegrationTestWebApplicationFactory factory)
{
    private readonly ProductsIntegrationTestWebApplicationFactory _factory = factory;

    [Fact]
    public async Task GetOpenApiDocument_ReturnsParseableDocumentDescribingTheProductsApi()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        using var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/openapi/v1.json", ct);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await response.Content.ReadAsStringAsync(ct);
        payload.Should().NotBeNullOrWhiteSpace();

        using var document = JsonDocument.Parse(payload);
        var root = document.RootElement;

        root.TryGetProperty("openapi", out var version).Should().BeTrue("the document must declare its OpenAPI version");
        version.GetString().Should().StartWith("3.", "the generator targets OpenAPI 3.x");

        root.TryGetProperty("info", out var info).Should().BeTrue();
        info.GetProperty("title").GetString().Should().Be("Template Products API");

        root.TryGetProperty("paths", out var paths).Should().BeTrue("the document must expose a paths object");
        paths.EnumerateObject().Should().NotBeEmpty("controllers should contribute at least one path");
        paths.TryGetProperty("/api/Products", out _)
            .Should().BeTrue("the products endpoint is part of the public contract the frontend client is generated from");

        var bearer = root
            .GetProperty("components")
            .GetProperty("securitySchemes")
            .GetProperty("Bearer");

        bearer.GetProperty("type").GetString().Should().Be("http");
        bearer.GetProperty("scheme").GetString().Should().Be("bearer");
        bearer.GetProperty("bearerFormat").GetString().Should().Be("JWT");
    }
}
