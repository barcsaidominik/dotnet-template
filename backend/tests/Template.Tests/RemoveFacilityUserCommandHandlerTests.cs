using ErrorOr;
using FluentAssertions;
using NSubstitute;
using Template.Application.Common.Dtos;
using Template.Application.Common.Interfaces;
using Template.Application.Facilities.Commands.RemoveFacilityUser;
using Template.Domain.Constants;
using Template.Domain.Errors;

namespace Template.Tests;

public class RemoveFacilityUserCommandHandlerTests
{
    private readonly IAuthService _authService;
    private readonly ICurrentUserService _currentUserService;
    private readonly RemoveFacilityUserCommandHandler _handler;

    public RemoveFacilityUserCommandHandlerTests()
    {
        _authService = Substitute.For<IAuthService>();
        _currentUserService = Substitute.For<ICurrentUserService>();
        _handler = new RemoveFacilityUserCommandHandler(_authService, _currentUserService);
    }

    [Fact]
    public async Task Handle_WithFacilityAdminAndMatchingFacilityId_DeletesUser()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var facilityId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var command = new RemoveFacilityUserCommand(facilityId, userId);

        _currentUserService.Role.Returns(Roles.FACILITY_ADMIN);
        _currentUserService.FacilityId.Returns(facilityId);
        _authService.GetUserByIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(new UserDto(userId, "user@test.com", facilityId, true, Roles.FACILITY_VIEWER));
        _authService.DeleteUserAsync(userId, Arg.Any<CancellationToken>())
            .Returns(Result.Success);

        // Act
        var result = await _handler.Handle(command, ct);

        // Assert
        result.IsError.Should().BeFalse();
        await _authService.Received(1).DeleteUserAsync(userId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithFacilityAdminAndWrongFacilityId_ReturnsAccessDenied()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var callerFacilityId = Guid.NewGuid();
        var targetFacilityId = Guid.NewGuid();
        var targetUserId = Guid.NewGuid();
        var command = new RemoveFacilityUserCommand(targetFacilityId, targetUserId);

        _currentUserService.Role.Returns(Roles.FACILITY_ADMIN);
        _currentUserService.FacilityId.Returns(callerFacilityId);
        _authService.GetUserByIdAsync(targetUserId, Arg.Any<CancellationToken>())
            .Returns(new UserDto(targetUserId, "victim@test.com", targetFacilityId, true, Roles.FACILITY_VIEWER));

        // Act
        var result = await _handler.Handle(command, ct);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Should().Be(FacilityErrors.AccessDenied);
        await _authService.DidNotReceive().DeleteUserAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithSystemAdmin_AllowsAnyFacilityAndDeletesUser()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var currentUserFacilityId = Guid.NewGuid();
        var requestedFacilityId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var command = new RemoveFacilityUserCommand(requestedFacilityId, userId);

        _currentUserService.Role.Returns(Roles.SYSTEM_ADMIN);
        _currentUserService.FacilityId.Returns(currentUserFacilityId);
        _authService.GetUserByIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(new UserDto(userId, "user@test.com", requestedFacilityId, true, Roles.FACILITY_VIEWER));
        _authService.DeleteUserAsync(userId, Arg.Any<CancellationToken>())
            .Returns(Result.Success);

        // Act
        var result = await _handler.Handle(command, ct);

        // Assert
        result.IsError.Should().BeFalse();
        await _authService.Received(1).DeleteUserAsync(userId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithTargetUserFromAnotherFacility_ReturnsUserNotInFacility()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var facilityId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var command = new RemoveFacilityUserCommand(facilityId, userId);

        _currentUserService.Role.Returns(Roles.FACILITY_ADMIN);
        _currentUserService.FacilityId.Returns(facilityId);
        _authService.GetUserByIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(new UserDto(userId, "other@test.com", Guid.NewGuid(), true, Roles.FACILITY_VIEWER));

        // Act
        var result = await _handler.Handle(command, ct);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Should().Be(FacilityErrors.UserNotInFacility);
        await _authService.DidNotReceive().DeleteUserAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }
}
