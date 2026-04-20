using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Template.Application.Common.Dtos;
using Template.Domain.Constants;

namespace Template.Tests;

[Collection("Api integration")]
public class FacilityUsersIntegrationTests(IntegrationTestWebApplicationFactory factory)
{
    private readonly IntegrationTestWebApplicationFactory _factory = factory;

    [Fact]
    public async Task GetFacilityUsers_WithFacilityAdmin_ReturnsUsersInFacility()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await _factory.ResetStateAsync(ct);
        using var client = await _factory.CreateAuthenticatedClientAsync(
            IntegrationTestWebApplicationFactory.FACILITY_ADMIN_EMAIL,
            IntegrationTestWebApplicationFactory.DEFAULT_PASSWORD,
            ct);

        // Act
        var response = await client.GetAsync($"/api/facilities/{_factory.FacilityId}/users", ct);
        var users = await response.Content.ReadFromJsonAsync<IReadOnlyList<UserDto>>(cancellationToken: ct);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        users.Should().NotBeNull();
        users!.Should().ContainSingle();
        users[0].Email.Should().Be(IntegrationTestWebApplicationFactory.FACILITY_ADMIN_EMAIL);
    }

    [Fact]
    public async Task GetFacilityUsers_WithSystemAdmin_ReturnsUsersInFacility()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await _factory.ResetStateAsync(ct);
        using var client = await _factory.CreateAuthenticatedClientAsync(
            IntegrationTestWebApplicationFactory.SYSTEM_ADMIN_EMAIL,
            IntegrationTestWebApplicationFactory.DEFAULT_PASSWORD,
            ct);

        // Act
        var response = await client.GetAsync($"/api/facilities/{_factory.FacilityId}/users", ct);
        var users = await response.Content.ReadFromJsonAsync<IReadOnlyList<UserDto>>(cancellationToken: ct);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        users.Should().NotBeNull();
        users!.Should().ContainSingle();
    }

    [Fact]
    public async Task CreateFacilityUser_WithFacilityAdmin_CreatesNewUser()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await _factory.ResetStateAsync(ct);
        using var client = await _factory.CreateAuthenticatedClientAsync(
            IntegrationTestWebApplicationFactory.FACILITY_ADMIN_EMAIL,
            IntegrationTestWebApplicationFactory.DEFAULT_PASSWORD,
            ct);

        var newUserEmail = "newuser@test.local";

        // Act
        var response = await client.PostAsJsonAsync(
            $"/api/facilities/{_factory.FacilityId}/users",
            new
            {
                Email = newUserEmail,
                Role = Roles.FACILITY_VIEWER
            },
            ct);
        var result = await response.Content.ReadFromJsonAsync<CreateUserResult>(cancellationToken: ct);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Should().NotBeNull();
        result!.UserId.Should().NotBeEmpty();
        result.SetupToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task CreateFacilityUser_WithDuplicateEmail_ReturnsConflict()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await _factory.ResetStateAsync(ct);
        using var client = await _factory.CreateAuthenticatedClientAsync(
            IntegrationTestWebApplicationFactory.FACILITY_ADMIN_EMAIL,
            IntegrationTestWebApplicationFactory.DEFAULT_PASSWORD,
            ct);

        // Act - try to create user with existing email
        var response = await client.PostAsJsonAsync(
            $"/api/facilities/{_factory.FacilityId}/users",
            new
            {
                Email = IntegrationTestWebApplicationFactory.FACILITY_ADMIN_EMAIL,
                Role = Roles.FACILITY_VIEWER
            },
            ct);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task RemoveFacilityUser_WithFacilityAdmin_RemovesUser()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await _factory.ResetStateAsync(ct);
        using var client = await _factory.CreateAuthenticatedClientAsync(
            IntegrationTestWebApplicationFactory.FACILITY_ADMIN_EMAIL,
            IntegrationTestWebApplicationFactory.DEFAULT_PASSWORD,
            ct);

        // First create a user to remove
        var createResponse = await client.PostAsJsonAsync(
            $"/api/facilities/{_factory.FacilityId}/users",
            new
            {
                Email = "toremove@test.local",
                Role = Roles.FACILITY_VIEWER
            },
            ct);
        var createResult = await createResponse.Content.ReadFromJsonAsync<CreateUserResult>(cancellationToken: ct);

        // Act
        var response = await client.DeleteAsync($"/api/facilities/{_factory.FacilityId}/users/{createResult!.UserId}", ct);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify user is removed
        var usersResponse = await client.GetAsync($"/api/facilities/{_factory.FacilityId}/users", ct);
        var users = await usersResponse.Content.ReadFromJsonAsync<IReadOnlyList<UserDto>>(cancellationToken: ct);
        users!.Should().NotContain(u => u.Email == "toremove@test.local");
    }

    [Fact]
    public async Task RemoveFacilityUser_WithNonExistentUser_ReturnsNotFound()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await _factory.ResetStateAsync(ct);
        using var client = await _factory.CreateAuthenticatedClientAsync(
            IntegrationTestWebApplicationFactory.FACILITY_ADMIN_EMAIL,
            IntegrationTestWebApplicationFactory.DEFAULT_PASSWORD,
            ct);

        var nonExistentUserId = Guid.NewGuid();

        // Act
        var response = await client.DeleteAsync($"/api/facilities/{_factory.FacilityId}/users/{nonExistentUserId}", ct);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateFacilityUserRole_WithFacilityAdmin_UpdatesRole()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await _factory.ResetStateAsync(ct);
        using var client = await _factory.CreateAuthenticatedClientAsync(
            IntegrationTestWebApplicationFactory.FACILITY_ADMIN_EMAIL,
            IntegrationTestWebApplicationFactory.DEFAULT_PASSWORD,
            ct);

        // First create a user
        var createResponse = await client.PostAsJsonAsync(
            $"/api/facilities/{_factory.FacilityId}/users",
            new
            {
                Email = "rolechange@test.local",
                Role = Roles.FACILITY_VIEWER
            },
            ct);
        var createResult = await createResponse.Content.ReadFromJsonAsync<CreateUserResult>(cancellationToken: ct);

        // Act
        var response = await client.PutAsJsonAsync(
            $"/api/facilities/{_factory.FacilityId}/users/{createResult!.UserId}/role",
            new
            {
                Role = Roles.FACILITY_EDITOR
            },
            ct);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UpdateFacilityUserRole_WithNonExistentUser_ReturnsNotFound()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await _factory.ResetStateAsync(ct);
        using var client = await _factory.CreateAuthenticatedClientAsync(
            IntegrationTestWebApplicationFactory.FACILITY_ADMIN_EMAIL,
            IntegrationTestWebApplicationFactory.DEFAULT_PASSWORD,
            ct);

        var nonExistentUserId = Guid.NewGuid();

        // Act
        var response = await client.PutAsJsonAsync(
            $"/api/facilities/{_factory.FacilityId}/users/{nonExistentUserId}/role",
            new
            {
                Role = Roles.FACILITY_EDITOR
            },
            ct);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
