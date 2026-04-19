using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Template.Application.Common.Interfaces;
using Template.Domain.Entities;
using Template.Domain.Errors;

namespace Template.Application.Admin.Commands.UpdateFacility;

public sealed class UpdateFacilityCommandHandler(IEntityStore<Facility> store) : IRequestHandler<UpdateFacilityCommand, ErrorOr<Updated>>
{
    private readonly IEntityStore<Facility> _store = store;

    public async ValueTask<ErrorOr<Updated>> Handle(UpdateFacilityCommand request, CancellationToken ct)
    {
        var facility = await _store.GetQuery()
            .FirstOrDefaultAsync(f => f.Id == request.FacilityId, ct);

        if (facility is null)
        {
            return FacilityErrors.NotFound;
        }

        var updateResult = facility.Update(request.Name);
        if (updateResult.IsError)
        {
            return updateResult.Errors;
        }

        await _store.SaveChangesAsync(ct);

        return Result.Updated;
    }
}
