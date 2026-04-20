using ErrorOr;
using Mediator;
using Template.Application.Common.Dtos;

namespace Template.Application.Admin.Queries.ExportUsersToExcel;

public sealed record ExportUsersToExcelQuery(
    string? Search = null,
    string? SortBy = null,
    bool SortDescending = false) : IRequest<ErrorOr<ExcelFileDto>>;
