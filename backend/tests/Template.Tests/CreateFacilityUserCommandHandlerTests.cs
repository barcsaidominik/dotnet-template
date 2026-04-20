using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Template.Application.Common.Dtos;
using Template.Application.Common.Interfaces;
using Template.Application.Common.Notifications;
using Template.Application.Facilities.Commands.CreateFacilityUser;
using Template.Domain.Constants;
using Template.Domain.Errors;

namespace Template.Tests;

public class CreateFacilityUserCommandHandlerTests
{
    private readonly IAuthService _authService;
    private readonly INotificationService _notificationService;
    private readonly IMailboxService _mailboxService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IFrontendSettings _frontendSettings;
    private readonly IMemoryCache _cache;
    private readonly ILogger<CreateFacilityUserCommandHandler> _logger;
    private readonly CreateFacilityUserCommandHandler _handler;

    public CreateFacilityUserCommandHandlerTests()
    {
        _authService = Substitute.For<IAuthService>();
        _notificationService = Substitute.For<INotificationService>();
        _mailboxService = Substitute.For<IMailboxService>();
        _currentUserService = Substitute.For<ICurrentUserService>();
        _frontendSettings = Substitute.For<IFrontendSettings>();
        _cache = Substitute.For<IMemoryCache>();
        _logger = Substitute.For<ILogger<CreateFacilityUserCommandHandler>>();

        _handler = new CreateFacilityUserCommandHandler(
            _authService,
            _notificationService,
            _mailboxService,
            _currentUserService,
            _frontendSettings,
            _cache,
            _logger);
    }

    [Fact]
    public async Task Handle_WithFacilityAdminAndMatchingFacilityId_DelegatesToService()
    {
        // Arrange
        var facilityId = Guid.NewGuid();
        var command = new CreateFacilityUserCommand(facilityId, "user@test.com", Roles.FACILITY_ADMIN);
        var createResult = new CreateUserResult(Guid.NewGuid(), "setup-token");

        _currentUserService.Role.Returns(Roles.FACILITY_ADMIN);
        _currentUserService.FacilityId.Returns(facilityId);
        _frontendSettings.BaseUrl.Returns("http://localhost:4200");
        _authService.CreateFacilityUserAsync(command.Email, command.FacilityId, command.Role, Arg.Any<CancellationToken>())
            .Returns(createResult);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        await _authService.Received(1).CreateFacilityUserAsync(command.Email, command.FacilityId, command.Role, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithFacilityAdminAndWrongFacilityId_ReturnsAccessDenied()
    {
        // Arrange
        var userFacilityId = Guid.NewGuid();
        var requestedFacilityId = Guid.NewGuid();
        var command = new CreateFacilityUserCommand(requestedFacilityId, "user@test.com", Roles.FACILITY_ADMIN);

        _currentUserService.Role.Returns(Roles.FACILITY_ADMIN);
        _currentUserService.FacilityId.Returns(userFacilityId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Should().Be(FacilityErrors.AccessDenied);
        await _authService.DidNotReceive().CreateFacilityUserAsync(Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithSystemAdmin_DelegatesToServiceRegardlessOfFacilityId()
    {
        // Arrange
        var requestedFacilityId = Guid.NewGuid();
        var command = new CreateFacilityUserCommand(requestedFacilityId, "user@test.com", Roles.FACILITY_ADMIN);
        var createResult = new CreateUserResult(Guid.NewGuid(), "setup-token");

        _currentUserService.Role.Returns(Roles.SYSTEM_ADMIN);
        _currentUserService.FacilityId.Returns((Guid?)null);
        _frontendSettings.BaseUrl.Returns("http://localhost:4200");
        _authService.CreateFacilityUserAsync(command.Email, command.FacilityId, command.Role, Arg.Any<CancellationToken>())
            .Returns(createResult);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        await _authService.Received(1).CreateFacilityUserAsync(command.Email, command.FacilityId, command.Role, Arg.Any<CancellationToken>());
    }
}
