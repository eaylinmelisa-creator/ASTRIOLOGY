using Microsoft.AspNetCore.OpenApi;

// Microsoft.OpenApi 2.x flattened the old Microsoft.OpenApi.Models namespace into the
// root namespace; importing Models instead fails to compile.
using Microsoft.OpenApi;

namespace Astriology.API.OpenApi;

/// <summary>
/// Declares the bearer scheme on the generated document so protected endpoints can
/// actually be exercised from the OpenAPI UI.
/// </summary>
/// <remarks>
/// Without this the document lists the endpoints but offers no way to attach a token,
/// which makes the smoke tests the roadmap relies on impossible to run by hand.
/// </remarks>
internal sealed class BearerSecuritySchemeTransformer : IOpenApiDocumentTransformer
{
    private const string SchemeName = "Bearer";

    public Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(document);

        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();

        document.Components.SecuritySchemes[SchemeName] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "Login yanıtındaki access token'ı buraya yapıştırın; 'Bearer ' öneki gerekmez.",
        };

        return Task.CompletedTask;
    }
}
