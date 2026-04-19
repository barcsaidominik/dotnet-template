using System.ComponentModel.DataAnnotations;

namespace Template.Api.Products;

public sealed class ProductsServiceOptions
{
    public const string SECTION_NAME = "ProductsService";
    public const string CLIENT_NAME = "ProductsServiceClient";
    public const string MODE_IN_PROCESS = "InProcess";
    public const string MODE_PROXY = "Proxy";

    public string Mode
    {
        get;
        set;
    } = MODE_IN_PROCESS;

    [Required]
    [Url]
    public string BaseUrl
    {
        get;
        set;
    } = "https://localhost:7135";

    [Required]
    [Url]
    public string GrpcBaseUrl
    {
        get;
        set;
    } = "http://localhost:7136";

    public bool UseProxyMode()
    {
        return string.Equals(Mode, MODE_PROXY, StringComparison.OrdinalIgnoreCase);
    }
}
