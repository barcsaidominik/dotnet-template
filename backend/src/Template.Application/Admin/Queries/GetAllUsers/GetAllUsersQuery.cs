using ErrorOr;
using Mediator;
using Template.Application.Common.Dtos;

namespace Template.Application.Admin.Queries.GetAllUsers;

public sealed record GetAllUsersQuery : IRequest<ErrorOr<IReadOnlyList<UserDto>>>;
