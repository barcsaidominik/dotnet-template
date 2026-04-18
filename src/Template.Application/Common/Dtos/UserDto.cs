namespace Template.Application.Common.Dtos;

public sealed record UserDto(Guid Id, string Email, Guid? FacilityId, bool IsApproved, string? Role);
