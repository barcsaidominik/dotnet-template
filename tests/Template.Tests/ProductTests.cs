using FluentAssertions;
using Template.Domain.Entities;
using Template.Domain.Errors;

namespace Template.Tests;

public class ProductTests
{
    [Fact]
    public void Create_WithValidInputs_ReturnsProduct()
    {
        // Arrange
        var name = "Test Product";
        var price = 99.99m;

        // Act
        var result = Product.Create(name, price);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Name.Should().Be(name);
        result.Value.Price.Should().Be(price);
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
        var result = Product.Create(name!, price);

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
        var result = Product.Create(name, price);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Should().Be(ProductErrors.InvalidPrice);
    }
}
