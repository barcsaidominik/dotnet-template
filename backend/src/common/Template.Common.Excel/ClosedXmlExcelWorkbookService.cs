using ClosedXML.Excel;
using ErrorOr;
using Template.Application.Common.Dtos;
using Template.Application.Common.Interfaces;

namespace Template.Common.Excel;

public sealed class ClosedXmlExcelWorkbookService : IExcelWorkbookService
{
    public byte[] ExportUsers(IReadOnlyList<UserExcelExportRowDto> rows)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Users");

        worksheet.Cell(1, 1).Value = "User ID";
        worksheet.Cell(1, 2).Value = "Email";
        worksheet.Cell(1, 3).Value = "Role";
        worksheet.Cell(1, 4).Value = "Approval Status";
        worksheet.Cell(1, 5).Value = "Facility ID";
        worksheet.Cell(1, 6).Value = "Preferred Language";

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

    public byte[] ExportFacilities(IReadOnlyList<FacilityExcelExportRowDto> rows)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Facilities");

        worksheet.Cell(1, 1).Value = "Facility ID";
        worksheet.Cell(1, 2).Value = "Name";

        for (var index = 0; index < rows.Count; index++)
        {
            var rowNumber = index + 2;
            var row = rows[index];

            worksheet.Cell(rowNumber, 1).Value = row.Id.ToString();
            worksheet.Cell(rowNumber, 2).Value = row.Name;
        }

        return BuildWorkbook(workbook, worksheet);
    }

    public byte[] ExportProducts(IReadOnlyList<ProductExcelExportRowDto> rows)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Products");

        worksheet.Cell(1, 1).Value = "Product ID";
        worksheet.Cell(1, 2).Value = "Name";
        worksheet.Cell(1, 3).Value = "Price";
        worksheet.Cell(1, 4).Value = "Facility ID";
        worksheet.Cell(1, 5).Value = "Created At (UTC)";

        for (var index = 0; index < rows.Count; index++)
        {
            var rowNumber = index + 2;
            var row = rows[index];

            worksheet.Cell(rowNumber, 1).Value = row.Id.ToString();
            worksheet.Cell(rowNumber, 2).Value = row.Name;
            worksheet.Cell(rowNumber, 3).Value = row.Price;
            worksheet.Cell(rowNumber, 4).Value = row.FacilityId.ToString();
            worksheet.Cell(rowNumber, 5).Value = row.CreatedAtUtc;
            worksheet.Cell(rowNumber, 5).Style.DateFormat.Format = "yyyy-mm-dd hh:mm:ss";
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

                rows.Add(new ProductExcelImportRowDto(rowNumber, name, price));
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
}
