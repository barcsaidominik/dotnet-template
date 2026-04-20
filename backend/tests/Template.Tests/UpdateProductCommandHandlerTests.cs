using ErrorOr;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Template.Application.Products.Commands.UpdateProduct;
using Template.Domain.Entities;
using Template.Domain.Errors;
using Template.Infrastructure.Persistence;

namespace Template.Tests;

public class UpdateProductCommandHandlerTests
{
    [Fact]
    public async Task Handle_WithExistingProduct_UpdatesAndReturnsUpdated()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var dbContext = CreateDbContext();
        var store = new EntityStore<Product>(dbContext, []);
        var handler = new UpdateProductCommandHandler(store);

        var facilityId = Guid.NewGuid();
        var productResult = Product.Create("Original", 10m, facilityId);
        productResult.IsError.Should().BeFalse();
        var product = productResult.Value;

        await dbContext.Products.AddAsync(product, ct);
        await dbContext.SaveChangesAsync(ct);

        var command = new UpdateProductCommand(product.Id, "Updated", 20m, 5);

        // Act
        var result = await handler.Handle(command, ct);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().Be(Result.Updated);

        var updatedProduct = await dbContext.Products.FirstOrDefaultAsync(p => p.Id == product.Id, ct);
        updatedProduct.Should().NotBeNull();
        updatedProduct!.Name.Should().Be("Updated");
        updatedProduct.Price.Should().Be(20m);
    }

    [Fact]
    public async Task Handle_WithNonExistentProduct_ReturnsNotFoundError()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var dbContext = CreateDbContext();
        var store = new EntityStore<Product>(dbContext, []);
        var handler = new UpdateProductCommandHandler(store);

        var command = new UpdateProductCommand(Guid.NewGuid(), "Updated", 20m, 5);

        // Act
        var result = await handler.Handle(command, ct);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Should().Be(ProductErrors.NotFound);
    }

    [Fact]
    public async Task Handle_WithInvalidName_ReturnsValidationError()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var dbContext = CreateDbContext();
        var store = new EntityStore<Product>(dbContext, []);
        var handler = new UpdateProductCommandHandler(store);

        var facilityId = Guid.NewGuid();
        var productResult = Product.Create("Original", 10m, facilityId);
        productResult.IsError.Should().BeFalse();
        var product = productResult.Value;

        await dbContext.Products.AddAsync(product, ct);
        await dbContext.SaveChangesAsync(ct);

        var command = new UpdateProductCommand(product.Id, "", 20m, 5);

        // Act
        var result = await handler.Handle(command, ct);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Should().Be(ProductErrors.InvalidName);

        var unchangedProduct = await dbContext.Products.FirstOrDefaultAsync(p => p.Id == product.Id, ct);
        unchangedProduct.Should().NotBeNull();
        unchangedProduct!.Name.Should().Be("Original");
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: $"UpdateProductTests-{Guid.NewGuid()}")
            .Options;

        return new AppDbContext(options);
    }
}
