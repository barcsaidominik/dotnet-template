using ErrorOr;
using Mediator;

namespace Template.Application.Admin.Commands.UpdateFacility;

public sealed record UpdateFacilityCommand(Guid FacilityId, string Name, uint RowVersion) : IRequest<ErrorOr<Updated>>;
