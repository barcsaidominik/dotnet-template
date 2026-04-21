using ErrorOr;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using NSubstitute;
using Template.Application.Admin.Commands.UpdateUser;
using Template.Application.Common.Dtos;
using Template.Application.Common.Interfaces;
using Template.Domain.Errors;

namespace Template.Tests;

public class UpdateUserCommandHandlerTests
{
    [Fact]
    public async Task Handle_WithExistingUser_UpdatesAndReturnsUpdated()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var userId = Guid.NewGuid();
        var newEmail = "updated@example.com";

        var authService = Substitute.For<IAuthService>();
        authService.UpdateUserAsync(userId, newEmail, ct).Returns(Result.Updated);

        var cache = new MemoryCache(new MemoryCacheOptions());
        var handler = new UpdateUserCommandHandler(authService, cache);

        var command = new UpdateUserCommand(userId, newEmail);

        // Act
        var result = await handler.Handle(command, ct);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().Be(Result.Updated);
        await authService.Received(1).UpdateUserAsync(userId, newEmail, ct);
    }

    [Fact]
    public async Task Handle_WithNonExistentUser_ReturnsNotFoundError()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var userId = Guid.NewGuid();
        var newEmail = "updated@example.com";

        var authService = Substitute.For<IAuthService>();
        authService.UpdateUserAsync(userId, newEmail, ct).Returns(AuthErrors.UserNotFound);

        var cache = new MemoryCache(new MemoryCacheOptions());
        var handler = new UpdateUserCommandHandler(authService, cache);

        var command = new UpdateUserCommand(userId, newEmail);

        // Act
        var result = await handler.Handle(command, ct);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Should().Be(AuthErrors.UserNotFound);
    }

    [Fact]
    public async Task Handle_WithDuplicateEmail_ReturnsUserAlreadyExistsError()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var userId = Guid.NewGuid();
        var duplicateEmail = "existing@example.com";

        var authService = Substitute.For<IAuthService>();
        authService.UpdateUserAsync(userId, duplicateEmail, ct).Returns(FacilityErrors.UserAlreadyExists);

        var cache = new MemoryCache(new MemoryCacheOptions());
        var handler = new UpdateUserCommandHandler(authService, cache);

        var command = new UpdateUserCommand(userId, duplicateEmail);

        // Act
        var result = await handler.Handle(command, ct);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Should().Be(FacilityErrors.UserAlreadyExists);
    }

    [Fact]
    public async Task Handle_OnSuccess_InvalidatesUsersCache()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var userId = Guid.NewGuid();
        var newEmail = "updated@example.com";

        var authService = Substitute.For<IAuthService>();
        authService.UpdateUserAsync(userId, newEmail, ct).Returns(Result.Updated);

        var cache = new MemoryCache(new MemoryCacheOptions());
        cache.Set("users:all", new List<UserDto>());
        var handler = new UpdateUserCommandHandler(authService, cache);

        var command = new UpdateUserCommand(userId, newEmail);

        // Act
        await handler.Handle(command, ct);

        // Assert
        cache.TryGetValue("users:all", out _).Should().BeFalse();
    }
}
