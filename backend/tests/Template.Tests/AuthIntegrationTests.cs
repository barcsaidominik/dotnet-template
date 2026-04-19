using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Template.Application.Common.Dtos;

namespace Template.Tests;

[Collection("Api integration")]
public class AuthIntegrationTests(IntegrationTestWebApplicationFactory factory)
{
    private readonly IntegrationTestWebApplicationFactory _factory = factory;

    [Fact]
    public async Task Login_WithApprovedFacilityAdmin_ReturnsTokenAndRefreshCookie()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await _factory.ResetStateAsync(ct);
        using var client = _factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            Email = IntegrationTestWebApplicationFactory.FACILITY_ADMIN_EMAIL,
            Password = IntegrationTestWebApplicationFactory.DEFAULT_PASSWORD
        }, ct);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken: ct);
        payload.Should().NotBeNull();
        payload!.Role.Should().Be("FacilityAdmin");
        payload.PreferredLanguage.Should().Be("hu-HU");
        response.Headers.TryGetValues("Set-Cookie", out var cookies).Should().BeTrue();
        cookies.Should().NotBeNull();
        cookies!.Any(cookie => cookie.Contains("refreshToken=", StringComparison.OrdinalIgnoreCase)).Should().BeTrue();
    }
}
