using ErrorOr;
using Mediator;
using Template.Application.Common.Dtos;

namespace Template.Application.Admin.Queries.ExportUsersToExcel;

public sealed record ExportUsersToExcelQuery : IRequest<ErrorOr<ExcelFileDto>>;
