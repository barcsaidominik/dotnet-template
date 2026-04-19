using ErrorOr;
using Template.Application.Common.Dtos;

namespace Template.Application.Common.Interfaces;

public interface IExcelWorkbookService
{
    byte[] ExportUsers(IReadOnlyList<UserExcelExportRowDto> rows);
    byte[] ExportFacilities(IReadOnlyList<FacilityExcelExportRowDto> rows);
    byte[] ExportProducts(IReadOnlyList<ProductExcelExportRowDto> rows);
    ErrorOr<IReadOnlyList<ProductExcelImportRowDto>> ImportProducts(byte[] content);
}
