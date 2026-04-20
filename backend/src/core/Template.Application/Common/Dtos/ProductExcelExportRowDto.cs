namespace Template.Application.Common.Dtos;

public sealed record ProductExcelExportRowDto(Guid Id, string Name, decimal Price, int Quantity, Guid FacilityId, DateTime CreatedAtUtc);
