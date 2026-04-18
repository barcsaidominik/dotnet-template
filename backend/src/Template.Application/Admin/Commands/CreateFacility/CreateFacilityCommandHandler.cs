using ErrorOr;
using Mediator;
using Template.Application.Common.Interfaces;
using Template.Domain.Entities;

namespace Template.Application.Admin.Commands.CreateFacility;

public sealed class CreateFacilityCommandHandler(IEntityStore<Facility> store) : IRequestHandler<CreateFacilityCommand, ErrorOr<Guid>>
{
    private readonly IEntityStore<Facility> _store = store;

    public async ValueTask<ErrorOr<Guid>> Handle(CreateFacilityCommand request, CancellationToken ct)
    {
        var facility = Facility.Create(request.Name);

        await _store.AddAsync(facility, ct);
        await _store.SaveChangesAsync(ct);

        return facility.Id;
    }
}
