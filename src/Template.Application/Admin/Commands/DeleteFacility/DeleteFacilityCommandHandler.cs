using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Template.Application.Common.Interfaces;
using Template.Domain.Entities;
using Template.Domain.Errors;

namespace Template.Application.Admin.Commands.DeleteFacility;

public sealed class DeleteFacilityCommandHandler : IRequestHandler<DeleteFacilityCommand, ErrorOr<Success>> {
    private readonly IEntityStore<Facility> _store;

    public DeleteFacilityCommandHandler(IEntityStore<Facility> store)
        => _store = store;

    public async ValueTask<ErrorOr<Success>> Handle(DeleteFacilityCommand request, CancellationToken cancellationToken) {
        var facility = await _store.GetQuery(skipGuards: true)
            .FirstOrDefaultAsync(f => f.Id == request.FacilityId, cancellationToken);

        if (facility is null) {
            return FacilityErrors.NotFound;
        }

        await _store.RemoveAsync(facility, cancellationToken);
        await _store.SaveChangesAsync(cancellationToken);

        return Result.Success;
    }
}
