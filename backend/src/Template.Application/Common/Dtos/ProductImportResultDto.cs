namespace Template.Application.Common.Dtos;

public sealed record ProductImportResultDto(
    int ImportedCount,
    int SkippedCount,
    IReadOnlyList<ExcelImportErrorDto> Errors);
