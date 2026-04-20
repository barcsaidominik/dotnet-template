using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Template.Application.Admin.Commands.DeleteFacility;
using Template.Application.Common.Interfaces;
using Template.Domain.Entities;
using Template.Domain.Errors;
using Template.Infrastructure.Persistence;

namespace Template.Tests;

public class DeleteFacilityCommandHandlerTests
{
    private readonly IFacilityProductUsageService _facilityProductUsageService;

    public DeleteFacilityCommandHandlerTests()
    {
        _facilityProductUsageService = Substitute.For<IFacilityProductUsageService>();
    }

    [Fact]
    public async Task Handle_WhenFacilityHasProducts_ReturnsConflictAndDoesNotDelete()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var dbContext = CreateDbContext();
        var store = new EntityStore<Facility>(dbContext, []);
        var handler = new DeleteFacilityCommandHandler(store, _facilityProductUsageService);

        var facility = Facility.Create("Delete Test Facility");
        await dbContext.Facilities.AddAsync(facility, ct);
        await dbContext.SaveChangesAsync(ct);

        _facilityProductUsageService.GetProductCountAsync(facility.Id, Arg.Any<CancellationToken>())
            .Returns(2);

        // Act
        var result = await handler.Handle(new DeleteFacilityCommand(facility.Id), ct);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Should().Be(FacilityErrors.HasProducts);
        dbContext.Facilities.Should().ContainSingle(f => f.Id == facility.Id);
    }

    [Fact]
    public async Task Handle_WhenFacilityHasNoProducts_DeletesFacility()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var dbContext = CreateDbContext();
        var store = new EntityStore<Facility>(dbContext, []);
        var handler = new DeleteFacilityCommandHandler(store, _facilityProductUsageService);

        var facility = Facility.Create("Delete Test Facility");
        await dbContext.Facilities.AddAsync(facility, ct);
        await dbContext.SaveChangesAsync(ct);

        _facilityProductUsageService.GetProductCountAsync(facility.Id, Arg.Any<CancellationToken>())
            .Returns(0);

        // Act
        var result = await handler.Handle(new DeleteFacilityCommand(facility.Id), ct);

        // Assert
        result.IsError.Should().BeFalse();
        dbContext.Facilities.Should().BeEmpty();
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: $"DeleteFacilityTests-{Guid.NewGuid()}")
            .Options;

        return new AppDbContext(options);
    }
}
