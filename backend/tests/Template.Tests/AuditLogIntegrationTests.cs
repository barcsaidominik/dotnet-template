using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Template.Application.Admin.Commands.CreateFacility;
using Template.Application.Common.Dtos;
using Template.Domain.Entities;
using Template.Domain.Enums;

namespace Template.Tests;

[Collection("Api integration")]
public class AuditLogIntegrationTests(IntegrationTestWebApplicationFactory factory)
{
    private readonly IntegrationTestWebApplicationFactory _factory = factory;

    [Fact]
    public async Task CreateFacility_ShouldCreateAuditEntry()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await _factory.ResetStateAsync(ct);
        using var client = await _factory.CreateAuthenticatedClientAsync(
            IntegrationTestWebApplicationFactory.SYSTEM_ADMIN_EMAIL,
            IntegrationTestWebApplicationFactory.DEFAULT_PASSWORD,
            ct);

        var command = new CreateFacilityCommand("Audit Test Facility");

        // Act
        var response = await client.PostAsJsonAsync("/api/admin/facilities", command, ct);
        var newFacilityId = await response.Content.ReadFromJsonAsync<Guid>(cancellationToken: ct);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var auditEntries = await _factory.ExecuteDbContextAsync(db =>
            db.Set<AuditEntry>()
                .Where(a => a.EntityType == "Facility" && a.EntityId == newFacilityId.ToString())
                .ToListAsync(ct));

        auditEntries.Should().ContainSingle();
        auditEntries[0].Action.Should().Be(AuditAction.Created);
        auditEntries[0].UserEmail.Should().Be(IntegrationTestWebApplicationFactory.SYSTEM_ADMIN_EMAIL);
        auditEntries[0].ChangesJson.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task UpdateFacility_ShouldCreateAuditEntry()
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

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var auditEntries = await _factory.ExecuteDbContextAsync(db =>
            db.Set<AuditEntry>()
                .Where(a => a.EntityType == "Facility"
                    && a.EntityId == _factory.FacilityId.ToString()
                    && a.Action == AuditAction.Updated)
                .ToListAsync(ct));

        auditEntries.Should().ContainSingle();
        auditEntries[0].UserEmail.Should().Be(IntegrationTestWebApplicationFactory.SYSTEM_ADMIN_EMAIL);
        auditEntries[0].ChangesJson.Should().Contain("Name");
    }

    [Fact]
    public async Task DeleteFacility_ShouldCreateAuditEntry()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await _factory.ResetStateAsync(ct);
        using var client = await _factory.CreateAuthenticatedClientAsync(
            IntegrationTestWebApplicationFactory.SYSTEM_ADMIN_EMAIL,
            IntegrationTestWebApplicationFactory.DEFAULT_PASSWORD,
            ct);

        // Create a facility to delete (don't delete the seeded one that has users)
        var createResponse = await client.PostAsJsonAsync(
            "/api/admin/facilities",
            new CreateFacilityCommand("Facility To Delete"),
            ct);
        var facilityToDeleteId = await createResponse.Content.ReadFromJsonAsync<Guid>(cancellationToken: ct);

        // Act
        var response = await client.DeleteAsync($"/api/admin/facilities/{facilityToDeleteId}", ct);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var auditEntries = await _factory.ExecuteDbContextAsync(db =>
            db.Set<AuditEntry>()
                .Where(a => a.EntityType == "Facility"
                    && a.EntityId == facilityToDeleteId.ToString()
                    && a.Action == AuditAction.Deleted)
                .ToListAsync(ct));

        auditEntries.Should().ContainSingle();
        auditEntries[0].UserEmail.Should().Be(IntegrationTestWebApplicationFactory.SYSTEM_ADMIN_EMAIL);
        auditEntries[0].ChangesJson.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetAuditLogs_FilterByEntityType_ReturnsCorrectEntries()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await _factory.ResetStateAsync(ct);
        using var client = await _factory.CreateAuthenticatedClientAsync(
            IntegrationTestWebApplicationFactory.SYSTEM_ADMIN_EMAIL,
            IntegrationTestWebApplicationFactory.DEFAULT_PASSWORD,
            ct);

        // Create multiple facilities to generate audit entries
        await client.PostAsJsonAsync(
            "/api/admin/facilities",
            new CreateFacilityCommand("Filter Test Facility 1"),
            ct);
        await client.PostAsJsonAsync(
            "/api/admin/facilities",
            new CreateFacilityCommand("Filter Test Facility 2"),
            ct);

        // Act
        var response = await client.GetAsync("/api/admin/audit?entityType=Facility", ct);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<AuditEntryDto>>(cancellationToken: ct);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Should().NotBeNull();
        result!.Items.Should().NotBeEmpty();
        result.Items.Should().AllSatisfy(item => item.EntityType.Should().Be("Facility"));
    }

    [Fact]
    public async Task GetAuditLogs_FilterByAction_ReturnsCorrectEntries()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await _factory.ResetStateAsync(ct);
        using var client = await _factory.CreateAuthenticatedClientAsync(
            IntegrationTestWebApplicationFactory.SYSTEM_ADMIN_EMAIL,
            IntegrationTestWebApplicationFactory.DEFAULT_PASSWORD,
            ct);

        // Create a facility (Created action)
        var createResponse = await client.PostAsJsonAsync(
            "/api/admin/facilities",
            new CreateFacilityCommand("Action Filter Facility"),
            ct);
        var facilityId = await createResponse.Content.ReadFromJsonAsync<Guid>(cancellationToken: ct);

        // Update the facility (Updated action)
        await client.PutAsJsonAsync(
            $"/api/admin/facilities/{facilityId}",
            new
            {
                Name = "Updated Action Filter Facility"
            },
            ct);

        // Act - filter by Created action (enum value = 1)
        var response = await client.GetAsync("/api/admin/audit?action=Created", ct);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<AuditEntryDto>>(cancellationToken: ct);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Should().NotBeNull();
        result!.Items.Should().AllSatisfy(item => item.Action.Should().Be("Created"));
    }

    [Fact]
    public async Task GetAuditLogs_Pagination_WorksCorrectly()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await _factory.ResetStateAsync(ct);
        using var client = await _factory.CreateAuthenticatedClientAsync(
            IntegrationTestWebApplicationFactory.SYSTEM_ADMIN_EMAIL,
            IntegrationTestWebApplicationFactory.DEFAULT_PASSWORD,
            ct);

        // Create multiple facilities to generate multiple audit entries
        for (var i = 1; i <= 5; i++)
        {
            await client.PostAsJsonAsync(
                "/api/admin/facilities",
                new CreateFacilityCommand($"Pagination Facility {i}"),
                ct);
        }

        // Act
        var response = await client.GetAsync("/api/admin/audit?page=1&pageSize=2", ct);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<AuditEntryDto>>(cancellationToken: ct);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Should().NotBeNull();
        result!.Items.Count.Should().Be(2);
        result.TotalCount.Should().BeGreaterThanOrEqualTo(5);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(2);

        // Verify second page
        var secondPageResponse = await client.GetAsync("/api/admin/audit?page=2&pageSize=2", ct);
        var secondPageResult = await secondPageResponse.Content.ReadFromJsonAsync<PagedResult<AuditEntryDto>>(cancellationToken: ct);

        secondPageResult.Should().NotBeNull();
        secondPageResult!.Items.Count.Should().Be(2);
        secondPageResult.Page.Should().Be(2);
    }

    [Fact]
    public async Task GetAuditLogs_FilterByDateRange_ReturnsCorrectEntries()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await _factory.ResetStateAsync(ct);
        using var client = await _factory.CreateAuthenticatedClientAsync(
            IntegrationTestWebApplicationFactory.SYSTEM_ADMIN_EMAIL,
            IntegrationTestWebApplicationFactory.DEFAULT_PASSWORD,
            ct);

        // Create a facility to generate an audit entry
        await client.PostAsJsonAsync(
            "/api/admin/facilities",
            new CreateFacilityCommand("Date Range Facility"),
            ct);

        var now = DateTime.UtcNow;
        var from = now.AddMinutes(-5).ToString("o");
        var to = now.AddMinutes(5).ToString("o");

        // Act
        var response = await client.GetAsync($"/api/admin/audit?from={Uri.EscapeDataString(from)}&to={Uri.EscapeDataString(to)}", ct);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<AuditEntryDto>>(cancellationToken: ct);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Should().NotBeNull();
        result!.Items.Should().NotBeEmpty();
        result.Items.Should().AllSatisfy(item =>
        {
            item.OccurredAt.Should().BeOnOrAfter(now.AddMinutes(-5));
            item.OccurredAt.Should().BeOnOrBefore(now.AddMinutes(5));
        });
    }

    [Fact]
    public async Task GetAuditLogs_WithFacilityAdmin_ReturnsForbidden()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await _factory.ResetStateAsync(ct);
        using var client = await _factory.CreateAuthenticatedClientAsync(
            IntegrationTestWebApplicationFactory.FACILITY_ADMIN_EMAIL,
            IntegrationTestWebApplicationFactory.DEFAULT_PASSWORD,
            ct);

        // Act
        var response = await client.GetAsync("/api/admin/audit", ct);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
