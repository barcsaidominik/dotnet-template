namespace Template.Application.Common.Dtos;

public sealed record LoginResult(string Token, DateTime ExpiresAt, string Role, string RefreshToken);
