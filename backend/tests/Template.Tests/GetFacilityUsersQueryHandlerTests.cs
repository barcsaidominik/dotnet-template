using FluentAssertions;
using NSubstitute;
using Template.Application.Common.Dtos;
using Template.Application.Common.Interfaces;
using Template.Application.Facilities.Queries.GetFacilityUsers;
using Template.Domain.Constants;
using Template.Domain.Errors;

namespace Template.Tests;

public class GetFacilityUsersQueryHandlerTests
{
    private readonly IAuthService _authService;
    private readonly ICurrentUserService _currentUserService;
    private readonly GetFacilityUsersQueryHandler _handler;

    public GetFacilityUsersQueryHandlerTests()
    {
        _authService = Substitute.For<IAuthService>();
        _currentUserService = Substitute.For<ICurrentUserService>();
        _handler = new GetFacilityUsersQueryHandler(_authService, _currentUserService);
    }

    [Fact]
    public async Task Handle_WithSystemAdmin_AllowsAnyFacilityAndDelegatesToService()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var currentUserFacilityId = Guid.NewGuid();
        var requestedFacilityId = Guid.NewGuid();
        var query = new GetFacilityUsersQuery(requestedFacilityId);
        var expectedUsers = new List<UserDto>();

        _currentUserService.Role.Returns(Roles.SYSTEM_ADMIN);
        _currentUserService.FacilityId.Returns(currentUserFacilityId);
        _authService.GetFacilityUsersAsync(requestedFacilityId, Arg.Any<CancellationToken>())
            .Returns(expectedUsers);

        // Act
        var result = await _handler.Handle(query, ct);

        // Assert
        result.IsError.Should().BeFalse();
        await _authService.Received(1).GetFacilityUsersAsync(requestedFacilityId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithFacilityAdminAndMatchingFacilityId_DelegatesToService()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var facilityId = Guid.NewGuid();
        var query = new GetFacilityUsersQuery(facilityId);
        var expectedUsers = new List<UserDto>();

        _currentUserService.Role.Returns(Roles.FACILITY_ADMIN);
        _currentUserService.FacilityId.Returns(facilityId);
        _authService.GetFacilityUsersAsync(facilityId, Arg.Any<CancellationToken>())
            .Returns(expectedUsers);

        // Act
        var result = await _handler.Handle(query, ct);

        // Assert
        result.IsError.Should().BeFalse();
        await _authService.Received(1).GetFacilityUsersAsync(facilityId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithFacilityAdminAndWrongFacilityId_ReturnsAccessDenied()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var userFacilityId = Guid.NewGuid();
        var requestedFacilityId = Guid.NewGuid();
        var query = new GetFacilityUsersQuery(requestedFacilityId);

        _currentUserService.Role.Returns(Roles.FACILITY_ADMIN);
        _currentUserService.FacilityId.Returns(userFacilityId);

        // Act
        var result = await _handler.Handle(query, ct);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Should().Be(FacilityErrors.AccessDenied);
        await _authService.DidNotReceive().GetFacilityUsersAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }
}
