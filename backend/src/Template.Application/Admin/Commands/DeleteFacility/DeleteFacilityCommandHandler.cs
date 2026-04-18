using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Template.Application.Common.Interfaces;
using Template.Domain.Entities;
using Template.Domain.Errors;

namespace Template.Application.Admin.Commands.DeleteFacility;

public sealed class DeleteFacilityCommandHandler(IEntityStore<Facility> store) : IRequestHandler<DeleteFacilityCommand, ErrorOr<Success>>
{
    private readonly IEntityStore<Facility> _store = store;

    public async ValueTask<ErrorOr<Success>> Handle(DeleteFacilityCommand request, CancellationToken ct)
    {
        var facility = await _store.GetQuery(skipGuards: true)
            .FirstOrDefaultAsync(f => f.Id == request.FacilityId, ct);

        if (facility is null)
        {
            return FacilityErrors.NotFound;
        }

        await _store.RemoveAsync(facility, ct);
        await _store.SaveChangesAsync(ct);

        return Result.Success;
    }
}
