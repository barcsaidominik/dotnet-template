namespace Template.Application.Common.Dtos;

public sealed record TokenResponse(string Token, DateTime ExpiresAt, string Role);
