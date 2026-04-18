using ErrorOr;
using Template.Domain.Common;
using Template.Domain.Errors;

namespace Template.Domain.Entities;

public class Product : Entity
{
    public string Name { get; private set; } = string.Empty;
    public decimal Price
    {
        get; private set;
    }
    public Guid FacilityId
    {
        get; private set;
    }

    private Product()
    {
    }

    public static ErrorOr<Product> Create(string name, decimal price, Guid facilityId)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return ProductErrors.InvalidName;
        }

        if (price <= 0)
        {
            return ProductErrors.InvalidPrice;
        }

        if (facilityId == Guid.Empty)
        {
            return ProductErrors.InvalidFacility;
        }

        return new Product
        {
            Name = name,
            Price = price,
            FacilityId = facilityId
        };
    }
}
