using ErrorOr;
using Mediator;

namespace Template.Application.Admin.Commands.DeleteFacility;

public sealed record DeleteFacilityCommand(Guid FacilityId) : IRequest<ErrorOr<Success>>;
