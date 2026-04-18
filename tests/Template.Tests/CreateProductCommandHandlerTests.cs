using FluentAssertions;
using NSubstitute;
using Template.Application.Common.Interfaces;
using Template.Application.Products.Commands.CreateProduct;
using Template.Domain.Entities;
using Template.Domain.Errors;

namespace Template.Tests;

public class CreateProductCommandHandlerTests
{
    private readonly IEntityStore<Product> _store;
    private readonly ICurrentUserService _currentUserService;
    private readonly CreateProductCommandHandler _handler;
    private readonly Guid _facilityId = Guid.NewGuid();

    public CreateProductCommandHandlerTests()
    {
        _store = Substitute.For<IEntityStore<Product>>();
        _currentUserService = Substitute.For<ICurrentUserService>();
        _currentUserService.FacilityId.Returns(_facilityId);
        _handler = new CreateProductCommandHandler(_store, _currentUserService);
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

        await _store.Received(1).AddAsync(Arg.Is<Product>(p =>
            p.Name == command.Name && p.Price == command.Price && p.FacilityId == _facilityId), Arg.Any<CancellationToken>());
        await _store.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
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

        await _store.DidNotReceive().AddAsync(Arg.Any<Product>(), Arg.Any<CancellationToken>());
        await _store.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
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

        await _store.DidNotReceive().AddAsync(Arg.Any<Product>(), Arg.Any<CancellationToken>());
        await _store.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithNoFacility_ReturnsForbiddenError()
    {
        // Arrange
        var command = new CreateProductCommand("Test Product", 50.00m);
        _currentUserService.FacilityId.Returns((Guid?)null);
        var handlerWithNoFacility = new CreateProductCommandHandler(_store, _currentUserService);

        // Act
        var result = await handlerWithNoFacility.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Product.NoFacility");

        await _store.DidNotReceive().AddAsync(Arg.Any<Product>(), Arg.Any<CancellationToken>());
        await _store.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
