namespace Template.Application.Common.Dtos;

public sealed record ProductExcelExportRowDto(Guid Id, string Name, decimal Price, Guid FacilityId, DateTime CreatedAtUtc);
