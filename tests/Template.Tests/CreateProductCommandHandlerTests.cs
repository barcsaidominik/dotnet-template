using FluentAssertions;
using NSubstitute;
using Template.Application.Common.Interfaces;
using Template.Application.Products.Commands.CreateProduct;
using Template.Domain.Entities;
using Template.Domain.Errors;

namespace Template.Tests;

public class CreateProductCommandHandlerTests
{
    private readonly IProductRepository _repository;
    private readonly CreateProductCommandHandler _handler;

    public CreateProductCommandHandlerTests()
    {
        _repository = Substitute.For<IProductRepository>();
        _handler = new CreateProductCommandHandler(_repository);
    }

    [Fact]
    public async Task Handle_WithValidCommand_CreatesProductAndReturnsId()
    {
        // Arrange
        var command = new CreateProductCommand("Test Product", 50.00m);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().NotBeEmpty();

        await _repository.Received(1).AddAsync(Arg.Is<Product>(p =>
            p.Name == command.Name && p.Price == command.Price), Arg.Any<CancellationToken>());
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithEmptyName_ReturnsValidationError()
    {
        // Arrange
        var command = new CreateProductCommand("", 50.00m);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Should().Be(ProductErrors.InvalidName);

        await _repository.DidNotReceive().AddAsync(Arg.Any<Product>(), Arg.Any<CancellationToken>());
        await _repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithNegativePrice_ReturnsValidationError()
    {
        // Arrange
        var command = new CreateProductCommand("Test Product", -10.00m);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Should().Be(ProductErrors.InvalidPrice);

        await _repository.DidNotReceive().AddAsync(Arg.Any<Product>(), Arg.Any<CancellationToken>());
        await _repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
