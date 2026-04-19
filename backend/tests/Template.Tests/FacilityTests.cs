using FluentAssertions;
using Template.Domain.Entities;
using Template.Domain.Errors;

namespace Template.Tests;

public class FacilityTests
{
    [Fact]
    public void Create_WithValidName_ReturnsFacility()
    {
        // Arrange
        const string name = "Central Facility";

        // Act
        var facility = Facility.Create(name);

        // Assert
        facility.Name.Should().Be(name);
        facility.Id.Should().NotBeEmpty();
        facility.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Update_WithValidName_UpdatesFacility()
    {
        // Arrange
        var facility = Facility.Create("Original Facility");
        const string updatedName = "Updated Facility";

        // Act
        var result = facility.Update(updatedName);

        // Assert
        result.IsError.Should().BeFalse();
        facility.Name.Should().Be(updatedName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Update_WithInvalidName_ReturnsError(string? name)
    {
        // Arrange
        var facility = Facility.Create("Original Facility");
        var originalName = facility.Name;

        // Act
        var result = facility.Update(name!);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Should().Be(FacilityErrors.InvalidName);
        facility.Name.Should().Be(originalName);
    }
}
