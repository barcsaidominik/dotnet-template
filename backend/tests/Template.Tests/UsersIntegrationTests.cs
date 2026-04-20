using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace Template.Tests;

[Collection("Api integration")]
public class UsersIntegrationTests(IntegrationTestWebApplicationFactory factory)
{
    private readonly IntegrationTestWebApplicationFactory _factory = factory;

    [Fact]
    public async Task UpdateLanguage_WithAuthenticatedUser_UpdatesPreference()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await _factory.ResetStateAsync(ct);
        using var client = await _factory.CreateAuthenticatedClientAsync(
            IntegrationTestWebApplicationFactory.FACILITY_ADMIN_EMAIL,
            IntegrationTestWebApplicationFactory.DEFAULT_PASSWORD,
            ct);

        // Act - use one of the supported languages (hu-HU or en-US)
        var response = await client.PutAsJsonAsync(
            "/api/users/me/language",
            new
            {
                Language = "en-US"
            },
            ct);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task UpdateLanguage_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await _factory.ResetStateAsync(ct);
        using var client = _factory.CreateClient();

        // Act
        var response = await client.PutAsJsonAsync(
            "/api/users/me/language",
            new
            {
                Language = "de-DE"
            },
            ct);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateLanguage_WithInvalidLanguage_ReturnsBadRequest()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await _factory.ResetStateAsync(ct);
        using var client = await _factory.CreateAuthenticatedClientAsync(
            IntegrationTestWebApplicationFactory.FACILITY_ADMIN_EMAIL,
            IntegrationTestWebApplicationFactory.DEFAULT_PASSWORD,
            ct);

        // Act - empty language should fail validation
        var response = await client.PutAsJsonAsync(
            "/api/users/me/language",
            new
            {
                Language = ""
            },
            ct);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
