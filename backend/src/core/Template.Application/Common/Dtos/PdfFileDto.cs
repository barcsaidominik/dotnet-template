namespace Template.Application.Common.Dtos;

public sealed record PdfFileDto(string FileName, string ContentType, byte[] Content);
