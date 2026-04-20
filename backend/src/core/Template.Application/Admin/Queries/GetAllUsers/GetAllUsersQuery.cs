using ErrorOr;
using Mediator;
using Template.Application.Common.Dtos;

namespace Template.Application.Admin.Queries.GetAllUsers;

public sealed record GetAllUsersQuery(
    string? Search = null,
    string? SortBy = null,
    bool SortDescending = false) : IRequest<ErrorOr<IReadOnlyList<UserDto>>>;
