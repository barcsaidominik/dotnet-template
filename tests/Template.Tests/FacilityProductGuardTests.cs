using FluentAssertions;
using NSubstitute;
using Template.Application.Common.Interfaces;
using Template.Application.Products.Guards;
using Template.Domain.Entities;

namespace Template.Tests;

public class FacilityProductGuardTests
{
    [Fact]
    public void Apply_WithFacilityId_FiltersProductsByFacilityId()
    {
        // Arrange
        var facilityId = Guid.NewGuid();
        var otherFacilityId = Guid.NewGuid();

        var currentUserService = Substitute.For<ICurrentUserService>();
        currentUserService.FacilityId.Returns(facilityId);

        var guard = new FacilityProductGuard(currentUserService);

        var products = new List<Product>
        {
            CreateProduct("Product 1", 10m, facilityId),
            CreateProduct("Product 2", 20m, facilityId),
            CreateProduct("Product 3", 30m, otherFacilityId)
        }.AsQueryable();

        // Act
        var result = guard.Apply(products);

        // Assert
        result.Should().HaveCount(2);
        result.All(p => p.FacilityId == facilityId).Should().BeTrue();
    }

    [Fact]
    public void Apply_WithNullFacilityId_ReturnsAllProducts()
    {
        // Arrange
        var facilityId1 = Guid.NewGuid();
        var facilityId2 = Guid.NewGuid();

        var currentUserService = Substitute.For<ICurrentUserService>();
        currentUserService.FacilityId.Returns((Guid?)null);

        var guard = new FacilityProductGuard(currentUserService);

        var products = new List<Product>
        {
            CreateProduct("Product 1", 10m, facilityId1),
            CreateProduct("Product 2", 20m, facilityId2)
        }.AsQueryable();

        // Act
        var result = guard.Apply(products);

        // Assert
        result.Should().HaveCount(2);
    }

    private static Product CreateProduct(string name, decimal price, Guid facilityId)
    {
        var result = Product.Create(name, price, facilityId);
        return result.Value;
    }
}
