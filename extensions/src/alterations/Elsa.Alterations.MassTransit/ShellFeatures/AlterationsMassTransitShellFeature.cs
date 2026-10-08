using CShells.Features;
using Elsa.Alterations.MassTransit.Consumers;
using Elsa.Alterations.MassTransit.Messages;
using Elsa.Alterations.MassTransit.Services;
using Elsa.ServiceBus.MassTransit.Extensions;
using Elsa.ServiceBus.MassTransit.ShellFeatures;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Alterations.MassTransit.ShellFeatures;

/// <summary>
/// Enables MassTransit-based alteration job dispatching.
/// </summary>
[ShellFeature(
    DisplayName = "MassTransit Alterations",
    Description = "Configures MassTransit-based alteration job dispatching",
    DependsOn = [typeof(MassTransitFeature)])]
[UsedImplicitly]
public class AlterationsMassTransitShellFeature(ShellFeatureContext context) : IShellFeature
{
    public void ConfigureServices(IServiceCollection services)
    {
        var queueName = KebabCaseEndpointNameFormatter.Instance.Consumer<RunAlterationJobConsumer>();
        var queueAddress = new Uri($"queue:elsa-{queueName}");
        EndpointConvention.Map<RunAlterationJob>(queueAddress);

        context.AddMassTransitConsumer<RunAlterationJobConsumer>(endpointName: queueName);

        services.AddScoped<MassTransitAlterationJobDispatcher>();
    }
}

