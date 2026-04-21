using ErrorOr;
using Mediator;
using Template.Application.Common.Dtos;
using Template.Application.Common.Interfaces;

namespace Template.Application.Admin.Queries.GetAuditLogs;

public sealed class GetAuditLogsQueryHandler(IAuditLogRepository repository)
    : IRequestHandler<GetAuditLogsQuery, ErrorOr<PagedResult<AuditEntryDto>>>
{
    private readonly IAuditLogRepository _repository = repository;

    public async ValueTask<ErrorOr<PagedResult<AuditEntryDto>>> Handle(GetAuditLogsQuery request, CancellationToken ct)
    {
        var result = await _repository.GetPagedAsync(
            request.EntityType,
            request.EntityId,
            request.Action,
            request.UserId,
            request.From,
            request.To,
            request.Page,
            request.PageSize,
            request.SortBy,
            request.SortDescending,
            ct);

        return result;
    }
}
