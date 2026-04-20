namespace Template.Application.Common.Dtos;

public sealed record ProductOrderPdfModel(
    Guid ProductId,
    string ProductName,
    decimal Price,
    int Quantity,
    Guid FacilityId,
    string FacilityName,
    string RequestedByEmail,
    DateTime GeneratedAtUtc);
