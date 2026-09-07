using System.Net;
using System.Text.Json;
using FluentAssertions;

namespace Template.Tests;

/// <summary>
/// Smoke tests for the generated OpenAPI document.
///
/// These exist to catch runtime breakage of the Microsoft.OpenApi dependency, which is pinned to
/// 2.7.5 in Template.Common to clear GHSA-v5pm-xwqc-g5wc while Microsoft.AspNetCore.OpenApi still
/// declares 2.0.0. A binary-incompatible pin does not fail the build -- it surfaces as a
/// MissingMethodException / TypeLoadException the first time a document is generated. The frontend
/// client generation consumes this document, so silent breakage here would poison the build chain.
/// </summary>
[Collection("Api integration")]
public class OpenApiDocumentIntegrationTests(IntegrationTestWebApplicationFactory factory)
{
    private readonly IntegrationTestWebApplicationFactory _factory = factory;

    [Fact]
    public async Task GetOpenApiDocument_ReturnsParseableDocumentDescribingTheApi()
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
        info.GetProperty("title").GetString().Should().Be("Template API");

        root.TryGetProperty("paths", out var paths).Should().BeTrue("the document must expose a paths object");
        paths.EnumerateObject().Should().NotBeEmpty("controllers should contribute at least one path");
        paths.TryGetProperty("/api/Auth/login", out _)
            .Should().BeTrue("the login endpoint is part of the public contract the frontend client is generated from");
    }

    [Fact]
    public async Task GetOpenApiDocument_AppliesBearerSecuritySchemeTransformer()
    {
        // Arrange -- BearerSecuritySchemeTransformer is the only first-party code that builds
        // Microsoft.OpenApi object graphs directly, so it is the most exposed to an API change
        // in the pinned package.
        var ct = TestContext.Current.CancellationToken;
        using var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/openapi/v1.json", ct);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));

        var bearer = document.RootElement
            .GetProperty("components")
            .GetProperty("securitySchemes")
            .GetProperty("Bearer");

        bearer.GetProperty("type").GetString().Should().Be("http");
        bearer.GetProperty("scheme").GetString().Should().Be("bearer");
        bearer.GetProperty("bearerFormat").GetString().Should().Be("JWT");
    }
}
