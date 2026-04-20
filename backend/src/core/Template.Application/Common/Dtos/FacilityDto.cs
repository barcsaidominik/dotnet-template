namespace Template.Application.Common.Dtos;

public sealed record FacilityDto(Guid Id, string Name);

public sealed record FacilityWithCountDto(Guid Id, string Name, int EmployeeCount);
