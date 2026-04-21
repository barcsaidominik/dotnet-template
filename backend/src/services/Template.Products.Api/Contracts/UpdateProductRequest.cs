namespace Template.Products.Api.Contracts;

public sealed record UpdateProductRequest(string Name, decimal Price, int Quantity, uint RowVersion);
