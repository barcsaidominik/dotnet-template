using FluentAssertions;
using NSubstitute;
using Template.Application.Auth.Commands.Login;
using Template.Application.Common.Dtos;
using Template.Application.Common.Interfaces;
using Template.Domain.Errors;

namespace Template.Tests;

public class LoginCommandHandlerTests
{
    private readonly IAuthService _authService;
    private readonly LoginCommandHandler _handler;

    public LoginCommandHandlerTests()
    {
        _authService = Substitute.For<IAuthService>();
        _handler = new LoginCommandHandler(_authService);
    }

    [Fact]
    public async Task Handle_WithValidCredentials_ReturnsLoginResult()
    {
        // Arrange
        var command = new LoginCommand("user@test.com", "Password123!");
        var expectedResult = new LoginResult("token", DateTime.UtcNow.AddHours(1), "FacilityAdmin", "refresh-token", "hu-HU");
        _authService.LoginAsync(command.Email, command.Password, Arg.Any<CancellationToken>())
            .Returns(expectedResult);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().Be(expectedResult);
    }

    [Fact]
    public async Task Handle_WithInvalidCredentials_ReturnsError()
    {
        // Arrange
        var command = new LoginCommand("user@test.com", "WrongPassword!");
        _authService.LoginAsync(command.Email, command.Password, Arg.Any<CancellationToken>())
            .Returns(AuthErrors.InvalidCredentials);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Should().Be(AuthErrors.InvalidCredentials);
    }

    [Fact]
    public async Task Handle_WithNotApprovedUser_ReturnsNotApprovedError()
    {
        // Arrange
        var command = new LoginCommand("user@test.com", "Password123!");
        _authService.LoginAsync(command.Email, command.Password, Arg.Any<CancellationToken>())
            .Returns(AuthErrors.NotApproved);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Should().Be(AuthErrors.NotApproved);
    }

    [Fact]
    public async Task Handle_WithPasswordChangeRequired_ReturnsPasswordChangeRequiredError()
    {
        // Arrange
        var command = new LoginCommand("user@test.com", "Password123!");
        _authService.LoginAsync(command.Email, command.Password, Arg.Any<CancellationToken>())
            .Returns(AuthErrors.PasswordChangeRequired);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Should().Be(AuthErrors.PasswordChangeRequired);
    }
}
