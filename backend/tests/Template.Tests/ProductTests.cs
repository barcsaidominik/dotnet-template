using FluentAssertions;
using Template.Domain.Entities;
using Template.Domain.Errors;

namespace Template.Tests;

public class ProductTests
{
    private readonly Guid _facilityId = Guid.NewGuid();

    [Fact]
    public void Create_WithValidInputs_ReturnsProduct()
    {
        // Arrange
        var name = "Test Product";
        var price = 99.99m;

        // Act
        var result = Product.Create(name, price, _facilityId);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Name.Should().Be(name);
        result.Value.Price.Should().Be(price);
        result.Value.FacilityId.Should().Be(_facilityId);
        result.Value.Id.Should().NotBeEmpty();
        result.Value.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithInvalidName_ReturnsError(string? name)
    {
        // Arrange
        var price = 99.99m;

        // Act
        var result = Product.Create(name!, price, _facilityId);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Should().Be(ProductErrors.InvalidName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100.50)]
    public void Create_WithInvalidPrice_ReturnsError(decimal price)
    {
        // Arrange
        var name = "Test Product";

        // Act
        var result = Product.Create(name, price, _facilityId);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Should().Be(ProductErrors.InvalidPrice);
    }

    [Fact]
    public void Create_WithEmptyFacilityId_ReturnsError()
    {
        // Arrange
        var name = "Test Product";
        var price = 99.99m;

        // Act
        var result = Product.Create(name, price, Guid.Empty);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Should().Be(ProductErrors.InvalidFacility);
    }

    [Fact]
    public void Update_WithValidInputs_UpdatesProduct()
    {
        // Arrange
        var product = Product.Create("Original Product", 25.50m, _facilityId).Value;
        var updatedName = "Updated Product";
        var updatedPrice = 31.75m;

        // Act
        var result = product.Update(updatedName, updatedPrice);

        // Assert
        result.IsError.Should().BeFalse();
        product.Name.Should().Be(updatedName);
        product.Price.Should().Be(updatedPrice);
    }

    [Theory]
    [InlineData(null, 25.50)]
    [InlineData("", 25.50)]
    [InlineData("   ", 25.50)]
    [InlineData("Updated Product", 0)]
    [InlineData("Updated Product", -10)]
    public void Update_WithInvalidInputs_ReturnsError(string? name, decimal price)
    {
        // Arrange
        var product = Product.Create("Original Product", 25.50m, _facilityId).Value;
        var originalName = product.Name;
        var originalPrice = product.Price;

        // Act
        var result = product.Update(name!, price);

        // Assert
        result.IsError.Should().BeTrue();
        product.Name.Should().Be(originalName);
        product.Price.Should().Be(originalPrice);
    }
}
