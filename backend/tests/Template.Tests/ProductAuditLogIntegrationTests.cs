using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Template.Application.Products.Commands.CreateProduct;
using Template.Domain.Entities;
using Template.Domain.Enums;

namespace Template.Tests;

[Collection("Products integration")]
public class ProductAuditLogIntegrationTests(ProductsIntegrationTestWebApplicationFactory factory)
{
    private readonly ProductsIntegrationTestWebApplicationFactory _factory = factory;

    [Fact]
    public async Task CreateProduct_ShouldCreateAuditEntry()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await _factory.ResetStateAsync(ct);
        using var client = _factory.CreateFacilityAdminClient();

        var command = new CreateProductCommand("Audit Test Product", 99.99m);

        // Act
        var response = await client.PostAsync(
            "/api/products",
            JsonContent.Create(command),
            ct);
        response.EnsureSuccessStatusCode();
        var newProductId = await response.Content.ReadFromJsonAsync<Guid>(cancellationToken: ct);

        // Assert
        var auditEntries = await _factory.ExecuteDbContextAsync(db =>
            db.Set<AuditEntry>()
                .Where(a => a.EntityType == "Product" && a.EntityId == newProductId.ToString())
                .ToListAsync(ct));

        auditEntries.Should().ContainSingle();
        auditEntries[0].Action.Should().Be(AuditAction.Created);
        auditEntries[0].UserId.Should().Be(_factory.FacilityAdminUserId);
        auditEntries[0].ChangesJson.Should().NotBeNullOrEmpty();
        auditEntries[0].ChangesJson.Should().Contain("Audit Test Product");
    }

    [Fact]
    public async Task UpdateProduct_ShouldCreateAuditEntry()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await _factory.ResetStateAsync(ct);
        using var client = _factory.CreateFacilityAdminClient();

        // Act
        var response = await client.PutAsync(
            $"/api/products/{_factory.SeededProductId}",
            JsonContent.Create(new
            {
                Name = "Updated Product Name",
                Price = 199.99m,
                Quantity = 10
            }),
            ct);
        response.EnsureSuccessStatusCode();

        // Assert
        var auditEntries = await _factory.ExecuteDbContextAsync(db =>
            db.Set<AuditEntry>()
                .Where(a => a.EntityType == "Product"
                    && a.EntityId == _factory.SeededProductId.ToString()
                    && a.Action == AuditAction.Updated)
                .ToListAsync(ct));

        auditEntries.Should().ContainSingle();
        auditEntries[0].UserId.Should().Be(_factory.FacilityAdminUserId);
        auditEntries[0].ChangesJson.Should().NotBeNullOrEmpty();
        auditEntries[0].ChangesJson.Should().Contain("Name");
    }
}
