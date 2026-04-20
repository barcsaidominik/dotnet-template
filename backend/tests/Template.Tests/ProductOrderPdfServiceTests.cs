using System.Globalization;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using NSubstitute;
using QuestPDF.Infrastructure;
using Template.Application.Common.Dtos;
using Template.Application.Common.Interfaces;
using Template.Common.Templating;
using Template.Infrastructure.Identity;
using Template.Infrastructure.Pdf;

namespace Template.Tests;

public class ProductOrderPdfServiceTests
{
    [Fact]
    public async Task GenerateAsync_WithAnonymousUser_RendersAllTemplatePartsAndReturnsPdf()
    {
        // Arrange
        QuestPDF.Settings.License = LicenseType.Community;
        var ct = TestContext.Current.CancellationToken;
        var templateRenderer = Substitute.For<ITemplateRenderer>();
        var currentUserService = Substitute.For<ICurrentUserService>();
        currentUserService.IsAuthenticated.Returns(false);
        currentUserService.UserId.Returns(Guid.Empty);

        templateRenderer
            .RenderAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<object>(),
                Arg.Any<CultureInfo?>(),
                Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.ArgAt<string>(2) switch
            {
                "title" => "Order Template",
                "subtitle" => "Requested order document",
                "notes" => "Notes block",
                "footer" => "Footer block",
                _ => string.Empty
            });

        var service = new ProductOrderPdfService(templateRenderer, currentUserService, CreateUserManager());
        var model = new ProductOrderPdfModel(
            Guid.NewGuid(),
            "Widget / Demo",
            42.75m,
            5,
            Guid.NewGuid(),
            "Central Facility",
            "admin@template.io",
            new DateTime(2026, 04, 19, 18, 45, 00, DateTimeKind.Utc));

        // Act
        var result = await service.GenerateAsync(model, ct);

        // Assert
        result.ContentType.Should().Be("application/pdf");
        result.FileName.Should().Be("Widget-Demo-order-template.pdf");
        result.Content.Should().NotBeEmpty();
        result.Content.Take(4).Should().Equal("%PDF"u8.ToArray());

        await templateRenderer.Received(4).RenderAsync(
            "Pdf",
            "product-order",
            Arg.Any<string>(),
            model,
            null,
            ct);
    }

    private static UserManager<AppUser> CreateUserManager()
    {
        var store = Substitute.For<IUserStore<AppUser>>();

        return new UserManager<AppUser>(
            store,
            null!,
            null!,
            [],
            [],
            null!,
            null!,
            null!,
            null!);
    }
}
