using ErrorOr;
using Mediator;
using Template.Application.Common.Dtos;

namespace Template.Application.Admin.Queries.ExportFacilitiesToExcel;

public sealed record ExportFacilitiesToExcelQuery : IRequest<ErrorOr<ExcelFileDto>>;
