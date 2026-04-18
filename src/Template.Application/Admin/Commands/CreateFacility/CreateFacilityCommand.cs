using ErrorOr;
using Mediator;

namespace Template.Application.Admin.Commands.CreateFacility;

public sealed record CreateFacilityCommand(string Name) : IRequest<ErrorOr<Guid>>;
