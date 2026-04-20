using System.Globalization;
using ErrorOr;
using Template.Application.Common.Dtos;

namespace Template.Application.Common.Interfaces;

public interface IExcelWorkbookService
{
    byte[] ExportUsers(IReadOnlyList<UserExcelExportRowDto> rows, CultureInfo? culture = null);
    byte[] ExportFacilities(IReadOnlyList<FacilityExcelExportRowDto> rows, CultureInfo? culture = null);
    byte[] ExportProducts(IReadOnlyList<ProductExcelExportRowDto> rows, CultureInfo? culture = null);
    ErrorOr<IReadOnlyList<ProductExcelImportRowDto>> ImportProducts(byte[] content);
}
