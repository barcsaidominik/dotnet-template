using ErrorOr;
using FluentAssertions;
using Mediator;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Template.Application.Common.Behaviors;
using Template.Application.Common.Interfaces;
using Template.Domain.Constants;
using Template.Domain.Errors;

namespace Template.Tests;

public class FacilityScopeBehaviorTests
{
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();

    [Fact]
    public async Task Handle_WithMatchingFacilityId_InvokesNext()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var facilityId = Guid.NewGuid();
        _currentUserService.Role.Returns(Roles.FACILITY_ADMIN);
        _currentUserService.FacilityId.Returns(facilityId);
        var behavior = new FacilityScopeBehavior<ScopedTestCommand, ErrorOr<string>>(_currentUserService);
        var nextWasCalled = false;

        // Act
        var result = await behavior.Handle(
            new ScopedTestCommand(facilityId),
            (message, cancellationToken) =>
            {
                nextWasCalled = true;
                return ValueTask.FromResult<ErrorOr<string>>("ok");
            },
            ct);

        // Assert
        nextWasCalled.Should().BeTrue();
        result.IsError.Should().BeFalse();
        result.Value.Should().Be("ok");
    }

    [Fact]
    public async Task Handle_WithForeignFacilityId_ReturnsAccessDeniedWithoutInvokingNext()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        _currentUserService.Role.Returns(Roles.FACILITY_ADMIN);
        _currentUserService.FacilityId.Returns(Guid.NewGuid());
        var behavior = new FacilityScopeBehavior<ScopedTestCommand, ErrorOr<string>>(_currentUserService);
        var nextWasCalled = false;

        // Act
        var result = await behavior.Handle(
            new ScopedTestCommand(Guid.NewGuid()),
            (message, cancellationToken) =>
            {
                nextWasCalled = true;
                return ValueTask.FromResult<ErrorOr<string>>("should-not-run");
            },
            ct);

        // Assert
        nextWasCalled.Should().BeFalse();
        result.IsError.Should().BeTrue();
        result.FirstError.Should().Be(FacilityErrors.AccessDenied);
    }

    [Fact]
    public async Task Handle_WithoutFacilityClaim_ReturnsAccessDeniedWithoutInvokingNext()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        _currentUserService.Role.Returns(Roles.FACILITY_ADMIN);
        _currentUserService.FacilityId.Returns((Guid?)null);
        var behavior = new FacilityScopeBehavior<ScopedTestCommand, ErrorOr<string>>(_currentUserService);
        var nextWasCalled = false;

        // Act
        var result = await behavior.Handle(
            new ScopedTestCommand(Guid.NewGuid()),
            (message, cancellationToken) =>
            {
                nextWasCalled = true;
                return ValueTask.FromResult<ErrorOr<string>>("should-not-run");
            },
            ct);

        // Assert
        nextWasCalled.Should().BeFalse();
        result.IsError.Should().BeTrue();
        result.FirstError.Should().Be(FacilityErrors.AccessDenied);
    }

    [Fact]
    public async Task Handle_WithSystemAdminAndForeignFacilityId_InvokesNext()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        _currentUserService.Role.Returns(Roles.SYSTEM_ADMIN);
        _currentUserService.FacilityId.Returns(Guid.NewGuid());
        var behavior = new FacilityScopeBehavior<ScopedTestCommand, ErrorOr<string>>(_currentUserService);
        var nextWasCalled = false;

        // Act
        var result = await behavior.Handle(
            new ScopedTestCommand(Guid.NewGuid()),
            (message, cancellationToken) =>
            {
                nextWasCalled = true;
                return ValueTask.FromResult<ErrorOr<string>>("ok");
            },
            ct);

        // Assert
        nextWasCalled.Should().BeTrue();
        result.IsError.Should().BeFalse();
    }

    [Fact]
    public void Resolve_AppliesBehaviorOnlyToFacilityScopedMessages()
    {
        // Arrange – mirrors the open generic registration in Template.Application.DependencyInjection
        var services = new ServiceCollection();
        services.AddSingleton(_currentUserService);
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(FacilityScopeBehavior<,>));
        using var provider = services.BuildServiceProvider();

        // Act
        var scopedBehaviors = provider.GetServices<IPipelineBehavior<ScopedTestCommand, ErrorOr<string>>>();
        var unscopedBehaviors = provider.GetServices<IPipelineBehavior<UnscopedTestCommand, ErrorOr<string>>>();

        // Assert
        scopedBehaviors.Should().ContainSingle(behavior => behavior is FacilityScopeBehavior<ScopedTestCommand, ErrorOr<string>>);
        unscopedBehaviors.Should().NotBeEmpty();
        unscopedBehaviors.Should().AllSatisfy(behavior => behavior.Should().BeOfType<ValidationBehavior<UnscopedTestCommand, ErrorOr<string>>>());
    }

    private sealed record ScopedTestCommand(Guid FacilityId) : IRequest<ErrorOr<string>>, IFacilityScopedRequest;

    private sealed record UnscopedTestCommand(string Name) : IRequest<ErrorOr<string>>;
}
