using ClosedXML.Excel;
using FluentAssertions;
using Template.Application.Common.Dtos;
using Template.Common.Excel;

namespace Template.Tests;

public class ClosedXmlExcelWorkbookServiceTests
{
    private readonly ClosedXmlExcelWorkbookService _service = new();

    [Fact]
    public void ExportProducts_WithRows_WritesExpectedWorksheetAndValues()
    {
        // Arrange
        var createdAtUtc = new DateTime(2026, 04, 19, 18, 30, 00, DateTimeKind.Utc);
        IReadOnlyList<ProductExcelExportRowDto> rows =
        [
            new(Guid.NewGuid(), "Widget", 19.99m, 10, Guid.NewGuid(), createdAtUtc)
        ];

        // Act
        var content = _service.ExportProducts(rows);

        // Assert
        using var workbook = new XLWorkbook(new MemoryStream(content));
        var worksheet = workbook.Worksheet("Products");

        worksheet.Cell(1, 1).GetString().Should().Be("Product ID");
        worksheet.Cell(1, 2).GetString().Should().Be("Name");
        worksheet.Cell(1, 3).GetString().Should().Be("Price");
        worksheet.Cell(1, 4).GetString().Should().Be("Quantity");
        worksheet.Cell(2, 1).GetString().Should().Be(rows[0].Id.ToString());
        worksheet.Cell(2, 2).GetString().Should().Be("Widget");
        worksheet.Cell(2, 3).GetValue<decimal>().Should().Be(19.99m);
        worksheet.Cell(2, 4).GetValue<int>().Should().Be(10);
        worksheet.Cell(2, 5).GetString().Should().Be(rows[0].FacilityId.ToString());
        worksheet.Cell(2, 6).GetDateTime().Should().Be(createdAtUtc);
    }

    [Fact]
    public void ImportProducts_WithValidWorkbook_ReturnsRowsAndSkipsBlankLines()
    {
        // Arrange
        var content = BuildWorkbook(worksheet =>
        {
            worksheet.Cell(1, 1).Value = "Name";
            worksheet.Cell(1, 2).Value = "Price";
            worksheet.Cell(2, 1).Value = "Widget";
            worksheet.Cell(2, 2).Value = 19.99m;
            worksheet.Cell(3, 1).Value = "";
            worksheet.Cell(3, 2).Value = "";
            worksheet.Cell(4, 1).Value = "Gadget";
            worksheet.Cell(4, 2).Value = 25.5m;
        });

        // Act
        var result = _service.ImportProducts(content);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().BeEquivalentTo(
        [
            new ProductExcelImportRowDto(2, "Widget", 19.99m),
            new ProductExcelImportRowDto(4, "Gadget", 25.5m)
        ]);
    }

    [Fact]
    public void ImportProducts_WithMissingName_ReturnsValidationError()
    {
        // Arrange
        var content = BuildWorkbook(worksheet =>
        {
            worksheet.Cell(1, 1).Value = "Name";
            worksheet.Cell(1, 2).Value = "Price";
            worksheet.Cell(2, 1).Value = "";
            worksheet.Cell(2, 2).Value = 19.99m;
        });

        // Act
        var result = _service.ImportProducts(content);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("ProductImport.NameRequired");
    }

    [Fact]
    public void ImportProducts_WithInvalidPrice_ReturnsValidationError()
    {
        // Arrange
        var content = BuildWorkbook(worksheet =>
        {
            worksheet.Cell(1, 1).Value = "Name";
            worksheet.Cell(1, 2).Value = "Price";
            worksheet.Cell(2, 1).Value = "Widget";
            worksheet.Cell(2, 2).Value = "not-a-number";
        });

        // Act
        var result = _service.ImportProducts(content);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("ProductImport.PriceInvalid");
    }

    [Fact]
    public void ImportProducts_WithInvalidBinaryContent_ReturnsValidationError()
    {
        // Arrange
        var content = new byte[] { 1, 2, 3, 4, 5 };

        // Act
        var result = _service.ImportProducts(content);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("ProductImport.InvalidWorkbook");
    }

    [Fact]
    public void ExportUsers_WithRows_WritesExpectedWorksheetAndValues()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var facilityId = Guid.NewGuid();
        IReadOnlyList<UserExcelExportRowDto> rows =
        [
            new(userId, "test@example.com", "Admin", "Approved", facilityId.ToString(), "en")
        ];

        // Act
        var content = _service.ExportUsers(rows);

        // Assert
        using var workbook = new XLWorkbook(new MemoryStream(content));
        var worksheet = workbook.Worksheet("Users");

        worksheet.Cell(1, 1).GetString().Should().Be("User ID");
        worksheet.Cell(1, 2).GetString().Should().Be("Email");
        worksheet.Cell(2, 1).GetString().Should().Be(userId.ToString());
        worksheet.Cell(2, 2).GetString().Should().Be("test@example.com");
        worksheet.Cell(2, 3).GetString().Should().Be("Admin");
        worksheet.Cell(2, 4).GetString().Should().Be("Approved");
        worksheet.Cell(2, 5).GetString().Should().Be(facilityId.ToString());
        worksheet.Cell(2, 6).GetString().Should().Be("en");
    }

    [Fact]
    public void ExportFacilities_WithRows_WritesExpectedWorksheetAndValues()
    {
        // Arrange
        var facilityId = Guid.NewGuid();
        IReadOnlyList<FacilityExcelExportRowDto> rows =
        [
            new(facilityId, "Test Facility")
        ];

        // Act
        var content = _service.ExportFacilities(rows);

        // Assert
        using var workbook = new XLWorkbook(new MemoryStream(content));
        var worksheet = workbook.Worksheet("Facilities");

        worksheet.Cell(1, 1).GetString().Should().Be("Facility ID");
        worksheet.Cell(1, 2).GetString().Should().Be("Name");
        worksheet.Cell(2, 1).GetString().Should().Be(facilityId.ToString());
        worksheet.Cell(2, 2).GetString().Should().Be("Test Facility");
    }

    private static byte[] BuildWorkbook(Action<IXLWorksheet> configureWorksheet)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.AddWorksheet("Import");
        configureWorksheet(worksheet);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
}
