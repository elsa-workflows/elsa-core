using Elsa.Features.Abstractions;
using Elsa.Features.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Common.Features;

/// <summary>
/// Adds and configures the Mediator feature.
/// </summary>
public class MediatorFeature : FeatureBase
{
    /// <inheritdoc />
    public MediatorFeature(IModule module) : base(module)
    {
    }

    /// <inheritdoc />
    public override void Apply()
    {
        Services
            .AddMediator()
            .AddMediatorHostedServices();
    }
}