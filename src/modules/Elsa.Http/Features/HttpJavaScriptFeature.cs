using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.Http.Scripting.JavaScript;
using Elsa.Scripting.JavaScript.Features;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Http.Features;

/// <summary>
/// Enabled when both <see cref="HttpFeature"/> and <see cref="JavaScriptFeature"/> are enabled.
/// </summary>
[DependencyOf(typeof(HttpFeature))]
[DependencyOf(typeof(JavaScriptFeature))]
public class HttpJavaScriptFeature : FeatureBase
{
    /// <inheritdoc />
    public HttpJavaScriptFeature(IModule module) : base(module)
    {
    }

    /// <inheritdoc />
    public override void Apply()
    {
        Services.AddNotificationHandler<HttpJavaScriptHandler>();
    }
}