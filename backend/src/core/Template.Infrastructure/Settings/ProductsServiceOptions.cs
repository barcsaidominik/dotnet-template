using System.ComponentModel.DataAnnotations;

namespace Template.Infrastructure.Settings;

public sealed class ProductsServiceOptions
{
    public const string SECTION_NAME = "ProductsService";

    [Required]
    [Url]
    public string GrpcBaseUrl
    {
        get;
        set;
    } = "http://localhost:7136";
}
