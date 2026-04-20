using ErrorOr;
using Mediator;
using Template.Application.Common.Dtos;

namespace Template.Application.Admin.Queries.ExportFacilitiesToExcel;

public sealed record ExportFacilitiesToExcelQuery(
    string? Search = null,
    string? SortBy = null,
    bool SortDescending = false) : IRequest<ErrorOr<ExcelFileDto>>;
