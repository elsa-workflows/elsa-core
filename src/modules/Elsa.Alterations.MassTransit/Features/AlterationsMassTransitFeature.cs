using Elsa.Alterations.Features;
using Elsa.Alterations.MassTransit.Consumers;
using Elsa.Alterations.MassTransit.Services;
using Elsa.Extensions;
using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.MassTransit.Features;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Alterations.MassTransit.Features;

/// <summary>
/// A feature for enabling the Alterations MassTransit feature.
/// </summary>
[DependsOn(typeof(MassTransitFeature))]
[DependsOn(typeof(AlterationsFeature))]
public class MassTransitAlterationsFeature : FeatureBase
{
    /// <inheritdoc />
    public MassTransitAlterationsFeature(IModule module) : base(module)
    {
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Module.Configure<AlterationsFeature>(feature => feature.AlterationJobDispatcherFactory = sp => sp.GetRequiredService<MassTransitAlterationJobDispatcher>());
        Module.AddMassTransitConsumer<RunAlterationJobConsumer>();
    }

    /// <inheritdoc />
    public override void Apply()
    {
        Services.AddSingleton<MassTransitAlterationJobDispatcher>();
    }
}