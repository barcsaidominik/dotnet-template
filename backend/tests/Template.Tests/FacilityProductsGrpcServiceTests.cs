using FluentAssertions;
using Grpc.Core;
using Mediator;
using Microsoft.Extensions.Options;
using NSubstitute;
using Template.Application.Products.Queries.GetFacilityProductCount;
using Template.Grpc.Products;
using Template.Infrastructure.Settings;
using Template.Products.Api.Services;

namespace Template.Tests;

public class FacilityProductsGrpcServiceTests
{
    private const string VALID_TOKEN = "valid-internal-service-token-12345678";
    private readonly ISender _sender;
    private readonly IOptions<InternalServiceAuthSettings> _authOptions;
    private readonly FacilityProductsGrpcService _service;

    public FacilityProductsGrpcServiceTests()
    {
        _sender = Substitute.For<ISender>();
        _authOptions = Options.Create(new InternalServiceAuthSettings { Token = VALID_TOKEN });
        _service = new FacilityProductsGrpcService(_sender, _authOptions);
    }

    [Fact]
    public async Task GetFacilityProductUsage_WithoutToken_ThrowsPermissionDenied()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var request = new FacilityProductUsageRequest { FacilityId = Guid.NewGuid().ToString() };
        var context = CreateServerCallContext(null, ct);

        // Act
        var act = () => _service.GetFacilityProductUsage(request, context);

        // Assert
        var exception = await act.Should().ThrowAsync<RpcException>();
        exception.Which.StatusCode.Should().Be(StatusCode.PermissionDenied);
    }

    [Fact]
    public async Task GetFacilityProductUsage_WithInvalidToken_ThrowsPermissionDenied()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var request = new FacilityProductUsageRequest { FacilityId = Guid.NewGuid().ToString() };
        var context = CreateServerCallContext("invalid-token", ct);

        // Act
        var act = () => _service.GetFacilityProductUsage(request, context);

        // Assert
        var exception = await act.Should().ThrowAsync<RpcException>();
        exception.Which.StatusCode.Should().Be(StatusCode.PermissionDenied);
    }

    [Fact]
    public async Task GetFacilityProductUsage_WithValidTokenAndInvalidFacilityId_ThrowsInvalidArgument()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var request = new FacilityProductUsageRequest { FacilityId = "not-a-guid" };
        var context = CreateServerCallContext(VALID_TOKEN, ct);

        // Act
        var act = () => _service.GetFacilityProductUsage(request, context);

        // Assert
        var exception = await act.Should().ThrowAsync<RpcException>();
        exception.Which.StatusCode.Should().Be(StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task GetFacilityProductUsage_WithValidTokenAndValidFacilityId_ReturnsSuccess()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var facilityId = Guid.NewGuid();
        var request = new FacilityProductUsageRequest { FacilityId = facilityId.ToString() };
        var context = CreateServerCallContext(VALID_TOKEN, ct);

        _sender.Send(Arg.Any<GetFacilityProductCountQuery>(), Arg.Any<CancellationToken>())
            .Returns(5);

        // Act
        var result = await _service.GetFacilityProductUsage(request, context);

        // Assert
        result.Should().NotBeNull();
        result.FacilityId.Should().Be(facilityId.ToString());
        result.ProductCount.Should().Be(5);
        result.HasProducts.Should().BeTrue();
    }

    [Fact]
    public async Task GetFacilityProductUsage_WithValidTokenAndZeroProducts_ReturnsHasProductsFalse()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var facilityId = Guid.NewGuid();
        var request = new FacilityProductUsageRequest { FacilityId = facilityId.ToString() };
        var context = CreateServerCallContext(VALID_TOKEN, ct);

        _sender.Send(Arg.Any<GetFacilityProductCountQuery>(), Arg.Any<CancellationToken>())
            .Returns(0);

        // Act
        var result = await _service.GetFacilityProductUsage(request, context);

        // Assert
        result.Should().NotBeNull();
        result.ProductCount.Should().Be(0);
        result.HasProducts.Should().BeFalse();
    }

    private static ServerCallContext CreateServerCallContext(string? token, CancellationToken ct)
    {
        var metadata = new Metadata();
        if (token is not null)
        {
            metadata.Add(InternalServiceAuthSettings.HEADER_NAME, token);
        }

        var context = Substitute.For<ServerCallContext>();
        context.RequestHeaders.Returns(metadata);
        context.CancellationToken.Returns(ct);
        return context;
    }
}
