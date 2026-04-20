using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Template.Common.OpenApi;

public sealed class BearerSecuritySchemeTransformer(IAuthenticationSchemeProvider authenticationSchemeProvider) : IOpenApiDocumentTransformer
{
    public async Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        var authenticationSchemes = await authenticationSchemeProvider.GetAllSchemesAsync();
        if (!authenticationSchemes.Any(scheme => scheme.Name == "Bearer"))
        {
            return;
        }

        var securityScheme = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Name = "Authorization",
            Description = "Enter your JWT token"
        };

        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes["Bearer"] = securityScheme;

        var securitySchemeReference = new OpenApiSecuritySchemeReference("Bearer");
        var securityRequirement = new OpenApiSecurityRequirement
        {
            { securitySchemeReference, new List<string>() }
        };

        if (document.Paths is null)
        {
            return;
        }

        foreach (var operation in document.Paths.Values.Where(path => path.Operations is not null).SelectMany(path => path.Operations!.Values))
        {
            operation.Security ??= [];
            operation.Security.Add(securityRequirement);
        }
    }
}
