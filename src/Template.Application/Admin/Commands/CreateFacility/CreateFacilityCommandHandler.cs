using ErrorOr;
using Mediator;
using Template.Application.Common.Interfaces;
using Template.Domain.Entities;

namespace Template.Application.Admin.Commands.CreateFacility;

public sealed class CreateFacilityCommandHandler : IRequestHandler<CreateFacilityCommand, ErrorOr<Guid>>
{
    private readonly IEntityStore<Facility> _store;

    public CreateFacilityCommandHandler(IEntityStore<Facility> store)
        => _store = store;

    public async ValueTask<ErrorOr<Guid>> Handle(CreateFacilityCommand request, CancellationToken cancellationToken)
    {
        var facility = Facility.Create(request.Name);

        await _store.AddAsync(facility, cancellationToken);
        await _store.SaveChangesAsync(cancellationToken);

        return facility.Id;
    }
}
