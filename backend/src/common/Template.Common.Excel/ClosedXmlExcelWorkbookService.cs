using System.Globalization;
using ClosedXML.Excel;
using ErrorOr;
using Template.Application.Common.Dtos;
using Template.Application.Common.Interfaces;

namespace Template.Common.Excel;

public sealed class ClosedXmlExcelWorkbookService : IExcelWorkbookService
{
    public byte[] ExportUsers(IReadOnlyList<UserExcelExportRowDto> rows, CultureInfo? culture = null)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Users");

        worksheet.Cell(1, 1).Value = L("UserID", culture);
        worksheet.Cell(1, 2).Value = L("Email", culture);
        worksheet.Cell(1, 3).Value = L("Role", culture);
        worksheet.Cell(1, 4).Value = L("ApprovalStatus", culture);
        worksheet.Cell(1, 5).Value = L("FacilityID", culture);
        worksheet.Cell(1, 6).Value = L("PreferredLanguage", culture);

        for (var index = 0; index < rows.Count; index++)
        {
            var rowNumber = index + 2;
            var row = rows[index];

            worksheet.Cell(rowNumber, 1).Value = row.Id.ToString();
            worksheet.Cell(rowNumber, 2).Value = row.Email;
            worksheet.Cell(rowNumber, 3).Value = row.Role;
            worksheet.Cell(rowNumber, 4).Value = row.ApprovalStatus;
            worksheet.Cell(rowNumber, 5).Value = row.FacilityId;
            worksheet.Cell(rowNumber, 6).Value = row.PreferredLanguage;
        }

        return BuildWorkbook(workbook, worksheet);
    }

    public byte[] ExportFacilities(IReadOnlyList<FacilityExcelExportRowDto> rows, CultureInfo? culture = null)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Facilities");

        worksheet.Cell(1, 1).Value = L("FacilityID", culture);
        worksheet.Cell(1, 2).Value = L("Name", culture);

        for (var index = 0; index < rows.Count; index++)
        {
            var rowNumber = index + 2;
            var row = rows[index];

            worksheet.Cell(rowNumber, 1).Value = row.Id.ToString();
            worksheet.Cell(rowNumber, 2).Value = row.Name;
        }

        return BuildWorkbook(workbook, worksheet);
    }

    public byte[] ExportProducts(IReadOnlyList<ProductExcelExportRowDto> rows, CultureInfo? culture = null)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Products");

        worksheet.Cell(1, 1).Value = L("ProductID", culture);
        worksheet.Cell(1, 2).Value = L("Name", culture);
        worksheet.Cell(1, 3).Value = L("Price", culture);
        worksheet.Cell(1, 4).Value = L("Quantity", culture);
        worksheet.Cell(1, 5).Value = L("FacilityID", culture);
        worksheet.Cell(1, 6).Value = L("CreatedAtUTC", culture);

        for (var index = 0; index < rows.Count; index++)
        {
            var rowNumber = index + 2;
            var row = rows[index];

            worksheet.Cell(rowNumber, 1).Value = row.Id.ToString();
            worksheet.Cell(rowNumber, 2).Value = row.Name;
            worksheet.Cell(rowNumber, 3).Value = row.Price;
            worksheet.Cell(rowNumber, 4).Value = row.Quantity;
            worksheet.Cell(rowNumber, 5).Value = row.FacilityId.ToString();
            worksheet.Cell(rowNumber, 6).Value = row.CreatedAtUtc;
            worksheet.Cell(rowNumber, 6).Style.DateFormat.Format = "yyyy-mm-dd hh:mm:ss";
        }

        return BuildWorkbook(workbook, worksheet);
    }

    public ErrorOr<IReadOnlyList<ProductExcelImportRowDto>> ImportProducts(byte[] content)
    {
        try
        {
            using var stream = new MemoryStream(content);
            using var workbook = new XLWorkbook(stream);
            var worksheet = workbook.Worksheets.FirstOrDefault();

            if (worksheet is null)
            {
                return Error.Validation("ProductImport.NoWorksheet", "The uploaded Excel file does not contain any worksheet.");
            }

            var rows = new List<ProductExcelImportRowDto>();

            foreach (var row in worksheet.RowsUsed().Skip(1))
            {
                var rowNumber = row.RowNumber();
                var name = row.Cell(1).GetString().Trim();
                var priceCell = row.Cell(2);
                var quantityCell = row.Cell(3);

                if (string.IsNullOrWhiteSpace(name) && priceCell.IsEmpty())
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(name))
                {
                    return Error.Validation("ProductImport.NameRequired", $"Row {rowNumber}: product name is required.");
                }

                if (!TryReadDecimal(priceCell, out var price))
                {
                    return Error.Validation("ProductImport.PriceInvalid", $"Row {rowNumber}: price must be a valid decimal number.");
                }

                var quantity = 0;
                if (!quantityCell.IsEmpty())
                {
                    if (!TryReadInt(quantityCell, out quantity))
                    {
                        return Error.Validation("ProductImport.QuantityInvalid", $"Row {rowNumber}: quantity must be a valid integer.");
                    }

                    if (quantity < 0)
                    {
                        return Error.Validation("ProductImport.QuantityNegative", $"Row {rowNumber}: quantity cannot be negative.");
                    }
                }

                rows.Add(new ProductExcelImportRowDto(rowNumber, name, price, quantity));
            }

            return rows.AsReadOnly();
        }
        catch (Exception)
        {
            return Error.Validation("ProductImport.InvalidWorkbook", "The uploaded file is not a valid Excel workbook.");
        }
    }

    private static byte[] BuildWorkbook(XLWorkbook workbook, IXLWorksheet worksheet)
    {
        var headerRange = worksheet.Range(
            worksheet.Cell(1, 1),
            worksheet.Cell(1, worksheet.LastColumnUsed()?.ColumnNumber() ?? 1));
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
        worksheet.SheetView.FreezeRows(1);
        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static bool TryReadDecimal(IXLCell cell, out decimal value)
    {
        if (cell.TryGetValue<decimal>(out value))
        {
            return true;
        }

        return decimal.TryParse(cell.GetString(), out value);
    }

    private static bool TryReadInt(IXLCell cell, out int value)
    {
        if (cell.TryGetValue<int>(out value))
        {
            return true;
        }

        return int.TryParse(cell.GetString(), out value);
    }

    private static string L(string key, CultureInfo? culture)
    {
        var isHungarian = culture?.Name == "hu-HU";

        return key switch
        {
            "UserID" => isHungarian ? "Felhaszn\u00e1l\u00f3 ID" : "User ID",
            "Email" => "Email",
            "Role" => isHungarian ? "Szerepk\u00f6r" : "Role",
            "ApprovalStatus" => isHungarian ? "J\u00f3v\u00e1hagy\u00e1si st\u00e1tusz" : "Approval Status",
            "FacilityID" => isHungarian ? "\u00dczem ID" : "Facility ID",
            "PreferredLanguage" => isHungarian ? "Prefer\u00e1lt nyelv" : "Preferred Language",
            "Name" => isHungarian ? "Megnevez\u00e9s" : "Name",
            "ProductID" => isHungarian ? "Term\u00e9k ID" : "Product ID",
            "Price" => isHungarian ? "\u00c1r" : "Price",
            "Quantity" => isHungarian ? "Mennyis\u00e9g" : "Quantity",
            "CreatedAtUTC" => isHungarian ? "L\u00e9trehozva (UTC)" : "Created At (UTC)",
            _ => key
        };
    }
}
