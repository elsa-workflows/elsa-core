using Elsa.Alterations.Features;
using Elsa.Alterations.MassTransit.Consumers;
using Elsa.Alterations.MassTransit.Messages;
using Elsa.Alterations.MassTransit.Services;
using Elsa.Extensions;
using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.MassTransit.Features;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Alterations.MassTransit.Features;

/// <summary>
/// A feature for enabling the Alterations MassTransit feature.
/// </summary>
/// <inheritdoc />
[DependsOn(typeof(MassTransitFeature))]
[DependsOn(typeof(AlterationsFeature))]
public class MassTransitAlterationsFeature(IModule module) : FeatureBase(module)
{

    /// <inheritdoc />
    public override void Configure()
    {
        Module.Configure<AlterationsFeature>(feature => feature.AlterationJobDispatcherFactory = sp => sp.GetRequiredService<MassTransitAlterationJobDispatcher>());
        var queueName = KebabCaseEndpointNameFormatter.Instance.Consumer<RunAlterationJobConsumer>();
        var queueAddress = new Uri($"queue:elsa-{queueName}");
        EndpointConvention.Map<RunAlterationJob>(queueAddress);
        Module.AddMassTransitConsumer<RunAlterationJobConsumer>(queueName);
    }

    /// <inheritdoc />
    public override void Apply() => Services.AddScoped<MassTransitAlterationJobDispatcher>();
}