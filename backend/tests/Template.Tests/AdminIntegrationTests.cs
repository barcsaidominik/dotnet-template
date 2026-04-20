using System.Net;
using System.Net.Http.Json;
using ClosedXML.Excel;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Template.Application.Admin.Commands.CreateFacility;
using Template.Application.Common.Dtos;
using Template.Domain.Constants;

namespace Template.Tests;

[Collection("Api integration")]
public class AdminIntegrationTests(IntegrationTestWebApplicationFactory factory)
{
    private readonly IntegrationTestWebApplicationFactory _factory = factory;

    [Fact]
    public async Task GetFacilities_WithSystemAdmin_ReturnsAllFacilities()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await _factory.ResetStateAsync(ct);
        using var client = await _factory.CreateAuthenticatedClientAsync(
            IntegrationTestWebApplicationFactory.SYSTEM_ADMIN_EMAIL,
            IntegrationTestWebApplicationFactory.DEFAULT_PASSWORD,
            ct);

        // Act
        var response = await client.GetAsync("/api/admin/facilities", ct);
        var facilities = await response.Content.ReadFromJsonAsync<IReadOnlyList<FacilityDto>>(cancellationToken: ct);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        facilities.Should().NotBeNull();
        facilities!.Should().ContainSingle();
        facilities[0].Name.Should().Be("Integration Facility");
    }

    [Fact]
    public async Task GetFacilities_WithFacilityAdmin_ReturnsForbidden()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await _factory.ResetStateAsync(ct);
        using var client = await _factory.CreateAuthenticatedClientAsync(
            IntegrationTestWebApplicationFactory.FACILITY_ADMIN_EMAIL,
            IntegrationTestWebApplicationFactory.DEFAULT_PASSWORD,
            ct);

        // Act
        var response = await client.GetAsync("/api/admin/facilities", ct);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreateFacility_WithSystemAdmin_CreatesNewFacility()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await _factory.ResetStateAsync(ct);
        using var client = await _factory.CreateAuthenticatedClientAsync(
            IntegrationTestWebApplicationFactory.SYSTEM_ADMIN_EMAIL,
            IntegrationTestWebApplicationFactory.DEFAULT_PASSWORD,
            ct);

        var command = new CreateFacilityCommand("New Test Facility");

        // Act
        var response = await client.PostAsJsonAsync("/api/admin/facilities", command, ct);
        var newFacilityId = await response.Content.ReadFromJsonAsync<Guid>(cancellationToken: ct);
        var createdFacility = await _factory.ExecuteDbContextAsync(
            dbContext => dbContext.Facilities.FirstOrDefaultAsync(f => f.Id == newFacilityId, ct));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        newFacilityId.Should().NotBeEmpty();
        createdFacility.Should().NotBeNull();
        createdFacility!.Name.Should().Be("New Test Facility");
    }

    [Fact]
    public async Task UpdateFacility_WithSystemAdmin_UpdatesFacilityName()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await _factory.ResetStateAsync(ct);
        using var client = await _factory.CreateAuthenticatedClientAsync(
            IntegrationTestWebApplicationFactory.SYSTEM_ADMIN_EMAIL,
            IntegrationTestWebApplicationFactory.DEFAULT_PASSWORD,
            ct);

        // Act
        var response = await client.PutAsJsonAsync(
            $"/api/admin/facilities/{_factory.FacilityId}",
            new
            {
                Name = "Updated Facility Name"
            },
            ct);

        var updatedFacility = await _factory.ExecuteDbContextAsync(
            dbContext => dbContext.Facilities.FirstOrDefaultAsync(f => f.Id == _factory.FacilityId, ct));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        updatedFacility.Should().NotBeNull();
        updatedFacility!.Name.Should().Be("Updated Facility Name");
    }

    [Fact]
    public async Task UpdateFacility_WithNonExistentId_ReturnsNotFound()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await _factory.ResetStateAsync(ct);
        using var client = await _factory.CreateAuthenticatedClientAsync(
            IntegrationTestWebApplicationFactory.SYSTEM_ADMIN_EMAIL,
            IntegrationTestWebApplicationFactory.DEFAULT_PASSWORD,
            ct);

        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await client.PutAsJsonAsync(
            $"/api/admin/facilities/{nonExistentId}",
            new
            {
                Name = "Updated Name"
            },
            ct);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteFacility_WithSystemAdmin_RemovesFacility()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await _factory.ResetStateAsync(ct);
        using var client = await _factory.CreateAuthenticatedClientAsync(
            IntegrationTestWebApplicationFactory.SYSTEM_ADMIN_EMAIL,
            IntegrationTestWebApplicationFactory.DEFAULT_PASSWORD,
            ct);

        // First create a facility to delete (don't delete the seeded one that has users)
        var createResponse = await client.PostAsJsonAsync(
            "/api/admin/facilities",
            new CreateFacilityCommand("Facility To Delete"),
            ct);
        var facilityToDeleteId = await createResponse.Content.ReadFromJsonAsync<Guid>(cancellationToken: ct);

        // Act
        var response = await client.DeleteAsync($"/api/admin/facilities/{facilityToDeleteId}", ct);
        var deletedFacility = await _factory.ExecuteDbContextAsync(
            dbContext => dbContext.Facilities.FirstOrDefaultAsync(f => f.Id == facilityToDeleteId, ct));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        deletedFacility.Should().BeNull();
    }

    [Fact]
    public async Task ExportFacilities_WithSystemAdmin_ReturnsExcelFile()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await _factory.ResetStateAsync(ct);
        using var client = await _factory.CreateAuthenticatedClientAsync(
            IntegrationTestWebApplicationFactory.SYSTEM_ADMIN_EMAIL,
            IntegrationTestWebApplicationFactory.DEFAULT_PASSWORD,
            ct);

        // Act
        var response = await client.GetAsync("/api/admin/facilities/export", ct);
        var content = await response.Content.ReadAsByteArrayAsync(ct);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");

        using var workbook = new XLWorkbook(new MemoryStream(content));
        workbook.Worksheets.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetUsers_WithSystemAdmin_ReturnsAllUsers()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await _factory.ResetStateAsync(ct);
        using var client = await _factory.CreateAuthenticatedClientAsync(
            IntegrationTestWebApplicationFactory.SYSTEM_ADMIN_EMAIL,
            IntegrationTestWebApplicationFactory.DEFAULT_PASSWORD,
            ct);

        // Act
        var response = await client.GetAsync("/api/admin/users", ct);
        var users = await response.Content.ReadFromJsonAsync<IReadOnlyList<UserDto>>(cancellationToken: ct);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        users.Should().NotBeNull();
        users!.Count.Should().BeGreaterThanOrEqualTo(2); // SystemAdmin + FacilityAdmin
        users.Should().Contain(u => u.Email == IntegrationTestWebApplicationFactory.SYSTEM_ADMIN_EMAIL);
        users.Should().Contain(u => u.Email == IntegrationTestWebApplicationFactory.FACILITY_ADMIN_EMAIL);
    }

    [Fact]
    public async Task ExportUsers_WithSystemAdmin_ReturnsExcelFile()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await _factory.ResetStateAsync(ct);
        using var client = await _factory.CreateAuthenticatedClientAsync(
            IntegrationTestWebApplicationFactory.SYSTEM_ADMIN_EMAIL,
            IntegrationTestWebApplicationFactory.DEFAULT_PASSWORD,
            ct);

        // Act
        var response = await client.GetAsync("/api/admin/users/export", ct);
        var content = await response.Content.ReadAsByteArrayAsync(ct);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");

        using var workbook = new XLWorkbook(new MemoryStream(content));
        workbook.Worksheets.Should().NotBeEmpty();
    }

    [Fact]
    public async Task DeleteUser_WithSystemAdmin_RemovesUser()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await _factory.ResetStateAsync(ct);
        using var client = await _factory.CreateAuthenticatedClientAsync(
            IntegrationTestWebApplicationFactory.SYSTEM_ADMIN_EMAIL,
            IntegrationTestWebApplicationFactory.DEFAULT_PASSWORD,
            ct);

        // Act
        var response = await client.DeleteAsync($"/api/admin/users/{_factory.FacilityAdminUserId}", ct);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task DeleteUser_WithNonExistentId_ReturnsNotFound()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await _factory.ResetStateAsync(ct);
        using var client = await _factory.CreateAuthenticatedClientAsync(
            IntegrationTestWebApplicationFactory.SYSTEM_ADMIN_EMAIL,
            IntegrationTestWebApplicationFactory.DEFAULT_PASSWORD,
            ct);

        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await client.DeleteAsync($"/api/admin/users/{nonExistentId}", ct);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ApproveUser_WithSystemAdmin_ApprovesUnapprovedUser()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await _factory.ResetStateAsync(ct);

        // Create an unapproved user via registration
        using var anonymousClient = _factory.CreateClient();
        var registerResponse = await anonymousClient.PostAsJsonAsync(
            "/api/auth/register",
            new
            {
                Email = "unapproved@test.local",
                Password = IntegrationTestWebApplicationFactory.DEFAULT_PASSWORD
            },
            ct);
        registerResponse.EnsureSuccessStatusCode();

        using var adminClient = await _factory.CreateAuthenticatedClientAsync(
            IntegrationTestWebApplicationFactory.SYSTEM_ADMIN_EMAIL,
            IntegrationTestWebApplicationFactory.DEFAULT_PASSWORD,
            ct);

        // Find the unapproved user
        var usersResponse = await adminClient.GetAsync("/api/admin/users", ct);
        var users = await usersResponse.Content.ReadFromJsonAsync<IReadOnlyList<UserDto>>(cancellationToken: ct);
        var unapprovedUser = users!.First(u => u.Email == "unapproved@test.local");

        // Act
        var response = await adminClient.PostAsJsonAsync(
            $"/api/admin/users/{unapprovedUser.Id}/approve",
            new
            {
                FacilityId = _factory.FacilityId,
                Role = Roles.FACILITY_VIEWER
            },
            ct);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
