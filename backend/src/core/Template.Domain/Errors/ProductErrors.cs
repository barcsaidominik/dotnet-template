using ErrorOr;

namespace Template.Domain.Errors;

public static class ProductErrors
{
    public static readonly Error NotFound = Error.NotFound("Product.NotFound", "Product not found");
    public static readonly Error InvalidName = Error.Validation("Product.InvalidName", "Name cannot be empty");
    public static readonly Error InvalidPrice = Error.Validation("Product.InvalidPrice", "Price must be positive");
    public static readonly Error InvalidFacility = Error.Validation("Product.InvalidFacility", "Facility ID must be provided");
    public static readonly Error InvalidQuantity = Error.Validation("Product.InvalidQuantity", "Quantity cannot be negative");
}
