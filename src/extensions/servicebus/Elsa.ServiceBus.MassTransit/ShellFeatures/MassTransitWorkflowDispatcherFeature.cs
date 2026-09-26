using CShells.Features;
using Elsa.ServiceBus.MassTransit.ConsumerDefinitions;
using Elsa.ServiceBus.MassTransit.Consumers;
using Elsa.ServiceBus.MassTransit.Extensions;
using Elsa.ServiceBus.MassTransit.Options;
using Elsa.ServiceBus.MassTransit.Services;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.ShellFeatures;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.ServiceBus.MassTransit.ShellFeatures;

/// <summary>
/// Shell feature that configures MassTransit as the workflow dispatcher implementation.
/// </summary>
[ShellFeature(
    DisplayName = "MassTransit Workflow Dispatcher",
    Description = "Uses MassTransit to dispatch workflows across the system",
    DependsOn = [typeof(WorkflowRuntimeFeature), typeof(MassTransitFeature)])]
[UsedImplicitly]
public class MassTransitWorkflowDispatcherFeature(ShellFeatureContext context) : IShellFeature
{
    public void ConfigureServices(IServiceCollection services)
    {
        context
            .AddMassTransitConsumer<DispatchWorkflowRequestConsumer, DispatchWorkflowRequestConsumerDefinition>()
            .AddMassTransitConsumer<DispatchCancelWorkflowsRequestConsumer>(endpointName: "elsa-dispatch-cancel-workflow", isTemporary: true)
            .AddMassTransitConsumer<DispatchStimulusRequestConsumer, DispatchStimulusRequestConsumerDefinition>(endpointName: "elsa-dispatch-stimulus");

        services.AddOptions<MassTransitWorkflowDispatcherOptions>();
        services.AddOptions<MassTransitStimulusDispatcherOptions>();
        services.AddScoped<MassTransitWorkflowCancellationDispatcher>();
        services.AddScoped<MassTransitStimulusDispatcher>();
        services.AddScoped<MassTransitWorkflowDispatcher>();
        services.AddScoped<ValidatingWorkflowDispatcher>();

        // Register as factory delegates that will be picked up by WorkflowRuntime.
        // Same decorator shape as core: Validating → Transactional → MassTransit.
        services.AddSingleton<Func<IServiceProvider, IWorkflowDispatcher>>(sp =>
        {
            var decoratedService = sp.GetRequiredService<MassTransitWorkflowDispatcher>();
            var transactionalService = ActivatorUtilities.CreateInstance<TransactionalWorkflowDispatcher>(sp, decoratedService);
            return ActivatorUtilities.CreateInstance<ValidatingWorkflowDispatcher>(sp, transactionalService);
        });
    }
}
