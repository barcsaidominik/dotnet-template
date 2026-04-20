using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Template.Application.Common.Dtos;

namespace Template.Tests;

[Collection("Api integration")]
public class AuthEdgeCaseIntegrationTests(IntegrationTestWebApplicationFactory factory)
{
    private readonly IntegrationTestWebApplicationFactory _factory = factory;

    [Fact]
    public async Task Register_WithDuplicateEmail_ReturnsConflict()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await _factory.ResetStateAsync(ct);
        using var client = _factory.CreateClient();

        // Act - try to register with an already existing email
        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            Email = IntegrationTestWebApplicationFactory.FACILITY_ADMIN_EMAIL,
            Password = IntegrationTestWebApplicationFactory.DEFAULT_PASSWORD
        }, ct);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Login_WithInvalidPassword_ReturnsUnauthorized()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await _factory.ResetStateAsync(ct);
        using var client = _factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            Email = IntegrationTestWebApplicationFactory.FACILITY_ADMIN_EMAIL,
            Password = "WrongPassword123!"
        }, ct);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_WithNonExistentUser_ReturnsUnauthorized()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await _factory.ResetStateAsync(ct);
        using var client = _factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            Email = "nonexistent@test.local",
            Password = IntegrationTestWebApplicationFactory.DEFAULT_PASSWORD
        }, ct);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_WithUnapprovedUser_ReturnsForbidden()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await _factory.ResetStateAsync(ct);
        using var client = _factory.CreateClient();

        // Register a new user (unapproved by default)
        var registerResponse = await client.PostAsJsonAsync("/api/auth/register", new
        {
            Email = "unapproved@test.local",
            Password = IntegrationTestWebApplicationFactory.DEFAULT_PASSWORD
        }, ct);
        registerResponse.EnsureSuccessStatusCode();

        // Act - try to login with unapproved user
        var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            Email = "unapproved@test.local",
            Password = IntegrationTestWebApplicationFactory.DEFAULT_PASSWORD
        }, ct);

        // Assert - unapproved users get 403 Forbidden (valid credentials but not approved)
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Register_WithValidCredentials_ReturnsOk()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await _factory.ResetStateAsync(ct);
        using var client = _factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            Email = "newuser@test.local",
            Password = IntegrationTestWebApplicationFactory.DEFAULT_PASSWORD
        }, ct);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Refresh_WithoutCookie_ReturnsUnauthorized()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await _factory.ResetStateAsync(ct);
        using var client = _factory.CreateClient();

        // Act
        var response = await client.PostAsync("/api/auth/refresh", null, ct);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Logout_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await _factory.ResetStateAsync(ct);
        using var client = _factory.CreateClient();

        // Act
        var response = await client.PostAsync("/api/auth/logout", null, ct);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Logout_WithAuthenticatedUser_ReturnsOk()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await _factory.ResetStateAsync(ct);
        using var client = await _factory.CreateAuthenticatedClientAsync(
            IntegrationTestWebApplicationFactory.FACILITY_ADMIN_EMAIL,
            IntegrationTestWebApplicationFactory.DEFAULT_PASSWORD,
            ct);

        // Act
        var response = await client.PostAsync("/api/auth/logout", null, ct);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Login_WithSystemAdmin_ReturnsTokenAndRole()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await _factory.ResetStateAsync(ct);
        using var client = _factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            Email = IntegrationTestWebApplicationFactory.SYSTEM_ADMIN_EMAIL,
            Password = IntegrationTestWebApplicationFactory.DEFAULT_PASSWORD
        }, ct);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken: ct);
        payload.Should().NotBeNull();
        payload!.Role.Should().Be("SystemAdmin");
        payload.PreferredLanguage.Should().Be("en-US");
    }
}
