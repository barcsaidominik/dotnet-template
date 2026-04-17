using ErrorOr;

namespace Template.Domain.Errors;

public static class ProductErrors
{
    public static readonly Error NotFound = Error.NotFound("Product.NotFound", "Product not found");
    public static readonly Error InvalidName = Error.Validation("Product.InvalidName", "Name cannot be empty");
    public static readonly Error InvalidPrice = Error.Validation("Product.InvalidPrice", "Price must be positive");
}
