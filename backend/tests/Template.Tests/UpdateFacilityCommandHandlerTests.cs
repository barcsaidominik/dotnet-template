using ErrorOr;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Template.Application.Admin.Commands.UpdateFacility;
using Template.Domain.Entities;
using Template.Domain.Errors;
using Template.Infrastructure.Persistence;

namespace Template.Tests;

public class UpdateFacilityCommandHandlerTests
{
    [Fact]
    public async Task Handle_WithExistingFacility_UpdatesAndReturnsUpdated()
    {
        // Arrange
        await using var dbContext = CreateDbContext();
        var store = new EntityStore<Facility>(dbContext, []);
        var handler = new UpdateFacilityCommandHandler(store);

        var facility = Facility.Create("Original");

        await dbContext.Facilities.AddAsync(facility);
        await dbContext.SaveChangesAsync();

        var command = new UpdateFacilityCommand(facility.Id, "Updated");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().Be(Result.Updated);

        var updatedFacility = await dbContext.Facilities.FirstOrDefaultAsync(f => f.Id == facility.Id);
        updatedFacility.Should().NotBeNull();
        updatedFacility!.Name.Should().Be("Updated");
    }

    [Fact]
    public async Task Handle_WithNonExistentFacility_ReturnsNotFoundError()
    {
        // Arrange
        await using var dbContext = CreateDbContext();
        var store = new EntityStore<Facility>(dbContext, []);
        var handler = new UpdateFacilityCommandHandler(store);

        var command = new UpdateFacilityCommand(Guid.NewGuid(), "Updated");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Should().Be(FacilityErrors.NotFound);
    }

    [Fact]
    public async Task Handle_WithInvalidName_ReturnsValidationError()
    {
        // Arrange
        await using var dbContext = CreateDbContext();
        var store = new EntityStore<Facility>(dbContext, []);
        var handler = new UpdateFacilityCommandHandler(store);

        var facility = Facility.Create("Original");

        await dbContext.Facilities.AddAsync(facility);
        await dbContext.SaveChangesAsync();

        var command = new UpdateFacilityCommand(facility.Id, "");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Should().Be(FacilityErrors.InvalidName);

        var unchangedFacility = await dbContext.Facilities.FirstOrDefaultAsync(f => f.Id == facility.Id);
        unchangedFacility.Should().NotBeNull();
        unchangedFacility!.Name.Should().Be("Original");
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: $"UpdateFacilityTests-{Guid.NewGuid()}")
            .Options;

        return new AppDbContext(options);
    }
}
