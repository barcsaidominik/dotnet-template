namespace Template.Products.Api.Contracts;

public sealed class ImportProductsRequest
{
    public required IFormFile File
    {
        get;
        init;
    }
}
