using ErrorOr;
using Mediator;
using Template.Application.Common.Dtos;

namespace Template.Application.Admin.Queries.GetAuditLogs;

public sealed record GetAuditLogsQuery(
    string? EntityType = null,
    string? EntityId = null,
    string? Action = null,
    Guid? UserId = null,
    DateTime? From = null,
    DateTime? To = null,
    int Page = 1,
    int PageSize = 50,
    string? SortBy = null,
    bool SortDescending = true) : IRequest<ErrorOr<PagedResult<AuditEntryDto>>>;
