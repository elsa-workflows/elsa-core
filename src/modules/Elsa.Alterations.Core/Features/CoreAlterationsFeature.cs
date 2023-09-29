using Elsa.Alterations.Core.Extensions;
using Elsa.Features.Abstractions;
using Elsa.Features.Services;

namespace Elsa.Alterations.Core.Features;

/// <summary>
/// Adds the core Elsa alterations services.
/// </summary>
public class CoreAlterationsFeature : FeatureBase
{
    /// <inheritdoc />
    public CoreAlterationsFeature(IModule module) : base(module)
    {
    }

    /// <inheritdoc />
    public override void Apply()
    {
        Services.AddAlterationsCore();
    }
}