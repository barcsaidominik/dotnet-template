namespace Template.Application.Common.Dtos;

public sealed record UserExcelExportRowDto(
    Guid Id,
    string Email,
    string Role,
    string ApprovalStatus,
    string FacilityId,
    string PreferredLanguage);
