using ErrorOr;
using FluentAssertions;
using FluentValidation;
using Mediator;
using Template.Application.Common.Behaviors;

namespace Template.Tests;

public class ValidationBehaviorTests
{
    [Fact]
    public async Task Handle_WithNoValidators_InvokesNext()
    {
        // Arrange
        var behavior = new ValidationBehavior<TestCommand, ErrorOr<string>>([]);
        var command = new TestCommand("Product", 2);
        var nextWasCalled = false;

        // Act
        var result = await behavior.Handle(
            command,
            (message, cancellationToken) =>
            {
                nextWasCalled = true;
                return ValueTask.FromResult<ErrorOr<string>>($"processed:{message.Name}:{message.Quantity}");
            },
            CancellationToken.None);

        // Assert
        nextWasCalled.Should().BeTrue();
        result.IsError.Should().BeFalse();
        result.Value.Should().Be("processed:Product:2");
    }

    [Fact]
    public async Task Handle_WithValidMessage_InvokesNext()
    {
        // Arrange
        IValidator<TestCommand>[] validators =
        [
            new TestCommandNameValidator(),
            new TestCommandQuantityValidator()
        ];
        var behavior = new ValidationBehavior<TestCommand, ErrorOr<string>>(validators);
        var command = new TestCommand("Product", 2);
        var nextWasCalled = false;

        // Act
        var result = await behavior.Handle(
            command,
            (message, cancellationToken) =>
            {
                nextWasCalled = true;
                return ValueTask.FromResult<ErrorOr<string>>($"ok:{message.Name}:{message.Quantity}");
            },
            CancellationToken.None);

        // Assert
        nextWasCalled.Should().BeTrue();
        result.IsError.Should().BeFalse();
        result.Value.Should().Be("ok:Product:2");
    }

    [Fact]
    public async Task Handle_WithValidationFailures_ReturnsValidationErrorsWithoutInvokingNext()
    {
        // Arrange
        IValidator<TestCommand>[] validators =
        [
            new TestCommandNameValidator(),
            new TestCommandQuantityValidator()
        ];
        var behavior = new ValidationBehavior<TestCommand, ErrorOr<string>>(validators);
        var command = new TestCommand(string.Empty, 0);
        var nextWasCalled = false;

        // Act
        var result = await behavior.Handle(
            command,
            (message, cancellationToken) =>
            {
                nextWasCalled = true;
                return ValueTask.FromResult<ErrorOr<string>>("should-not-run");
            },
            CancellationToken.None);

        // Assert
        nextWasCalled.Should().BeFalse();
        result.IsError.Should().BeTrue();
        result.Errors.Should().Contain(error =>
            error.Type == ErrorType.Validation &&
            error.Code == nameof(TestCommand.Name) &&
            error.Description == "NAME_REQUIRED");
        result.Errors.Should().Contain(error =>
            error.Type == ErrorType.Validation &&
            error.Code == nameof(TestCommand.Quantity) &&
            error.Description == "QUANTITY_POSITIVE");
        result.Errors
            .Select(error => new { error.Type, error.Code, error.Description })
            .Distinct()
            .Should()
            .HaveCount(2);
    }

    private sealed record TestCommand(string Name, int Quantity) : IRequest<ErrorOr<string>>;

    private sealed class TestCommandNameValidator : AbstractValidator<TestCommand>
    {
        public TestCommandNameValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .WithErrorCode("NAME_REQUIRED");
        }
    }

    private sealed class TestCommandQuantityValidator : AbstractValidator<TestCommand>
    {
        public TestCommandQuantityValidator()
        {
            RuleFor(x => x.Quantity)
                .GreaterThan(0)
                .WithErrorCode("QUANTITY_POSITIVE");
        }
    }
}
