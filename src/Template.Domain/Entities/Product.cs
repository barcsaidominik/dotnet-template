using ErrorOr;
using Template.Domain.Common;
using Template.Domain.Errors;

namespace Template.Domain.Entities;

public class Product : Entity
{
    public string Name { get; private set; } = string.Empty;
    public decimal Price { get; private set; }

    private Product() { }

    public static ErrorOr<Product> Create(string name, decimal price)
    {
        if (string.IsNullOrWhiteSpace(name))
            return ProductErrors.InvalidName;
        if (price <= 0)
            return ProductErrors.InvalidPrice;

        return new Product
        {
            Name = name,
            Price = price
        };
    }
}
