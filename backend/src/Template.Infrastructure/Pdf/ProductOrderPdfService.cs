using System.Globalization;
using Microsoft.AspNetCore.Identity;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Template.Application.Common.Dtos;
using Template.Application.Common.Interfaces;
using Template.Infrastructure.Identity;
using Template.Infrastructure.Templating;

namespace Template.Infrastructure.Pdf;

public sealed class ProductOrderPdfService(
    ITemplateRenderer templateRenderer,
    ICurrentUserService currentUserService,
    UserManager<AppUser> userManager) : IProductOrderPdfService
{
    private const string TEMPLATE_GROUP = "Pdf";
    private const string TEMPLATE_NAME = "product-order";
    private const string CONTENT_TYPE = "application/pdf";

    private readonly ITemplateRenderer _templateRenderer = templateRenderer;
    private readonly ICurrentUserService _currentUserService = currentUserService;
    private readonly UserManager<AppUser> _userManager = userManager;

    public async Task<PdfFileDto> GenerateAsync(ProductOrderPdfModel model, CancellationToken ct = default)
    {
        var culture = await ResolveCultureAsync(ct);
        var title = await _templateRenderer.RenderAsync(TEMPLATE_GROUP, TEMPLATE_NAME, "title", model, culture, ct);
        var subtitle = await _templateRenderer.RenderAsync(TEMPLATE_GROUP, TEMPLATE_NAME, "subtitle", model, culture, ct);
        var notes = await _templateRenderer.RenderAsync(TEMPLATE_GROUP, TEMPLATE_NAME, "notes", model, culture, ct);
        var footer = await _templateRenderer.RenderAsync(TEMPLATE_GROUP, TEMPLATE_NAME, "footer", model, culture, ct);

        var content = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(11));

                page.Header().Column(column =>
                {
                    column.Spacing(6);
                    column.Item().Text(title).Bold().FontSize(22).FontColor(Colors.Blue.Darken2);
                    column.Item().Text(subtitle).FontSize(11).FontColor(Colors.Grey.Darken1);
                });

                page.Content().PaddingVertical(20).Column(column =>
                {
                    column.Spacing(18);

                    column.Item().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(16).Column(info =>
                    {
                        info.Spacing(8);
                        info.Item().Text($"{model.ProductName}").SemiBold().FontSize(16);
                        info.Item().Text($"Product ID: {model.ProductId}");
                        info.Item().Text($"Facility: {model.FacilityName} ({model.FacilityId})");
                        info.Item().Text($"Requested by: {model.RequestedByEmail}");
                        info.Item().Text($"Generated at (UTC): {model.GeneratedAtUtc:yyyy-MM-dd HH:mm:ss}");
                    });

                    column.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(4);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(2);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Element(CellStyle).Text("Product");
                            header.Cell().Element(CellStyle).AlignRight().Text("Quantity");
                            header.Cell().Element(CellStyle).AlignRight().Text("Unit Price");
                            header.Cell().Element(CellStyle).AlignRight().Text("Total");
                        });

                        table.Cell().Element(ValueCellStyle).Text(model.ProductName);
                        table.Cell().Element(ValueCellStyle).AlignRight().Text("1");
                        table.Cell().Element(ValueCellStyle).AlignRight().Text($"{model.Price:F2}");
                        table.Cell().Element(ValueCellStyle).AlignRight().Text($"{model.Price:F2}");
                    });

                    column.Item().Background(Colors.Grey.Lighten4).Padding(16).Text(notes);
                });

                page.Footer().AlignCenter().Text(footer).FontSize(9).FontColor(Colors.Grey.Darken1);
            });
        }).GeneratePdf();

        var safeProductName = string.Join("-", model.ProductName
            .Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));

        return new PdfFileDto(
            $"{safeProductName}-order-template.pdf",
            CONTENT_TYPE,
            content);
    }

    private async Task<CultureInfo?> ResolveCultureAsync(CancellationToken ct)
    {
        if (!_currentUserService.IsAuthenticated || _currentUserService.UserId == Guid.Empty)
        {
            return null;
        }

        var user = await _userManager.FindByIdAsync(_currentUserService.UserId.ToString());
        if (user?.PreferredLanguage is null)
        {
            return null;
        }

        return CultureInfo.GetCultureInfo(user.PreferredLanguage);
    }

    private static IContainer CellStyle(IContainer container)
    {
        return container
            .BorderBottom(1)
            .BorderColor(Colors.Grey.Lighten1)
            .Background(Colors.Blue.Lighten5)
            .PaddingVertical(8)
            .PaddingHorizontal(10);
    }

    private static IContainer ValueCellStyle(IContainer container)
    {
        return container
            .BorderBottom(1)
            .BorderColor(Colors.Grey.Lighten2)
            .PaddingVertical(8)
            .PaddingHorizontal(10);
    }
}
