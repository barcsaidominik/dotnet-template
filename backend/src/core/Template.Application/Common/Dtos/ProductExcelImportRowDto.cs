namespace Template.Application.Common.Dtos;

public sealed record ProductExcelImportRowDto(int RowNumber, string Name, decimal Price, int Quantity = 0);
