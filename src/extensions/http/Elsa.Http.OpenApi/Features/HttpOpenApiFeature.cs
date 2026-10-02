using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.Http.Features;
using Elsa.Http.OpenApi.Extensions;

namespace Elsa.Http.OpenApi.Features;

/// <summary>
/// Feature for HTTP OpenAPI functionality.
/// </summary>
[DependsOn(typeof(HttpFeature))]
public class HttpOpenApiFeature : FeatureBase
{
    /// <inheritdoc />
    public HttpOpenApiFeature(IModule module) : base(module)
    {
    }

    /// <inheritdoc />
    public override void Apply()
    {
        Services.AddElsaHttpOpenApi();
    }
}
