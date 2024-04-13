using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.Http.Contracts;
using Elsa.Http.Handlers;
using Elsa.Http.Services;
using Elsa.Workflows.Management.Features;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Http.Features;

/// <summary>
/// Installs services related to HTTP services and activities.
/// </summary>
[DependsOn(typeof(HttpFeature))]
[DependsOn(typeof(CachingWorkflowDefinitionsFeature))]
public class HttpCacheFeature : FeatureBase
{
    /// <inheritdoc />
    public HttpCacheFeature(IModule module) : base(module)
    {
    }

    /// <inheritdoc />
    public override void Configure()
    {
    }

    /// <inheritdoc />
    public override void Apply()
    {
        Services
            .AddSingleton<IHttpWorkflowsCacheManager, HttpWorkflowsCacheManager>()
            .Decorate<IHttpWorkflowLookupService, CachingHttpWorkflowLookupService>()
            .AddNotificationHandler<InvalidateHttpWorkflowsCache>();
    }
}