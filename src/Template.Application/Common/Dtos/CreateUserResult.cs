namespace Template.Application.Common.Dtos;

public sealed record CreateUserResult(Guid UserId, string SetupToken);
