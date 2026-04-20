using System.Globalization;
using Microsoft.AspNetCore.Identity;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Template.Application.Common.Dtos;
using Template.Application.Common.Interfaces;
using Template.Common.Templating;
using Template.Infrastructure.Identity;

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
                        info.Item().Text($"{GetTranslatedString("ProductID", culture)} {model.ProductId}");
                        info.Item().Text($"{GetTranslatedString("Facility", culture)} {model.FacilityName} ({model.FacilityId})");
                        info.Item().Text($"{GetTranslatedString("RequestedBy", culture)} {model.RequestedByEmail}");
                        info.Item().Text($"{GetTranslatedString("GeneratedAt", culture)} {model.GeneratedAtUtc:yyyy-MM-dd HH:mm:ss}");
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
                            header.Cell().Element(CellStyle).Text(GetTranslatedString("Product", culture));
                            header.Cell().Element(CellStyle).AlignRight().Text(GetTranslatedString("Quantity", culture));
                            header.Cell().Element(CellStyle).AlignRight().Text(GetTranslatedString("UnitPrice", culture));
                            header.Cell().Element(CellStyle).AlignRight().Text(GetTranslatedString("Total", culture));
                        });

                        var total = model.Price * model.Quantity;

                        table.Cell().Element(ValueCellStyle).Text(model.ProductName);
                        table.Cell().Element(ValueCellStyle).AlignRight().Text(model.Quantity.ToString());
                        table.Cell().Element(ValueCellStyle).AlignRight().Text($"{model.Price:F2}");
                        table.Cell().Element(ValueCellStyle).AlignRight().Text($"{total:F2}");
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

    private static string GetTranslatedString(string key, CultureInfo? culture)
    {
        var isHungarian = culture?.Name == "hu-HU";

        return key switch
        {
            "ProductID" => isHungarian ? "Term\u00e9k ID:" : "Product ID:",
            "Facility" => isHungarian ? "\u00dczem:" : "Facility:",
            "RequestedBy" => isHungarian ? "K\u00e9rte:" : "Requested by:",
            "GeneratedAt" => isHungarian ? "Gener\u00e1lva (UTC):" : "Generated at (UTC):",
            "Product" => isHungarian ? "Term\u00e9k" : "Product",
            "Quantity" => isHungarian ? "Mennyis\u00e9g" : "Quantity",
            "UnitPrice" => isHungarian ? "Egys\u00e9g\u00e1r" : "Unit Price",
            "Total" => isHungarian ? "\u00d6sszesen" : "Total",
            _ => key
        };
    }
}
