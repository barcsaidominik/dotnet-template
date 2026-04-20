namespace Template.Application.Common.Dtos;

public sealed record ExcelFileDto(string FileName, string ContentType, byte[] Content);
