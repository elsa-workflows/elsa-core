using Elsa.Alterations.Core.Features;
using Elsa.Alterations.Extensions;
using Elsa.Extensions;
using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Alterations.Features;

/// <summary>
/// Adds the Elsa alterations services.
/// </summary>
[DependsOn(typeof(CoreAlterationsFeature))]
public class AlterationsFeature : FeatureBase
{
    /// <inheritdoc />
    public AlterationsFeature(IModule module) : base(module)
    {
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Module.AddFastEndpointsAssembly<AlterationsFeature>();
    }

    /// <inheritdoc />
    public override void Apply()
    {
        Services.AddAlterations();
        Services.AddNotificationHandlersFrom<AlterationsFeature>();
    }
}