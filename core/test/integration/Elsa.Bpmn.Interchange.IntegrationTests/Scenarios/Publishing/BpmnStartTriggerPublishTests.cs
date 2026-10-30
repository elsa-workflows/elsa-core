using Bpmn.Model;
using Elsa.Bpmn.Activities;
using Elsa.Mediator.Contracts;
using Elsa.Workflows;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Filters;
using Elsa.Workflows.Runtime.Handlers;
using Elsa.Workflows.Runtime.Stimuli;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace Elsa.Bpmn.Interchange.IntegrationTests.Scenarios.Publishing;

/// <summary>
/// Publishing an imported BPMN process through the real <see cref="Elsa.Workflows.Management.IWorkflowDefinitionPublisher"/>,
/// with the runtime's <see cref="ValidateWorkflowRequestHandler"/> in the pipeline exactly as <c>WorkflowRuntimeFeature</c>
/// registers it by default (#8078). That handler refuses publication for every stored trigger without a payload, so
/// a process whose start events declare nothing to register must not leave one behind, while a process whose start
/// events do declare something must still register it.
/// </summary>
public class BpmnStartTriggerPublishTests(ITestOutputHelper testOutputHelper) : BpmnPublishGateTestBase(testOutputHelper)
{
    [Fact(DisplayName = "The runtime's trigger validation is part of the publish these tests go through")]
    public void TriggerValidationIsRegistered()
    {
        // Every other test here passes vacuously without this handler: it is the one that turns a stored trigger
        // without a payload into a refused publication.
        Assert.Contains(Services.GetServices<INotificationHandler>(), handler => handler is ValidateWorkflowRequestHandler);
    }

    [Fact(DisplayName = "An imported process whose only start is a plain start event publishes, and stores no trigger")]
    public async Task PlainStartProcess_Publishes_AndStoresNoTrigger()
    {
        var definitionId = await ImportAsync("camunda-order-process.bpmn");

        var result = await PublishAsync(definitionId);

        Assert.True(result.Succeeded, string.Join("; ", result.ValidationErrors.Select(error => error.Message)));
        Assert.Empty(await StoredTriggersAsync(definitionId));
    }

    [Fact(DisplayName = "An imported process with a message start publishes, and stores the trigger an event publisher matches")]
    public async Task MessageStartProcess_Publishes_AndIndexesItsTrigger()
    {
        var definitionId = await ImportAsync("message-start-order-process.bpmn");

        var result = await PublishAsync(definitionId);

        Assert.True(result.Succeeded, string.Join("; ", result.ValidationErrors.Select(error => error.Message)));

        var trigger = Assert.Single(await StoredTriggersAsync(definitionId));
        var stimulus = new EventStimulus("OrderPlaced");
        Assert.Equal(RuntimeStimulusNames.Event, trigger.Name);
        Assert.Equal(stimulus, trigger.Payload);
        Assert.Equal(Services.GetRequiredService<IStimulusHasher>().Hash(RuntimeStimulusNames.Event, stimulus), trigger.Hash);
    }

    [Fact(DisplayName = "A root process whose only start is event-defined but registers nothing still fails publication")]
    public async Task UnresolvableEventDefinedStart_StillFailsPublication()
    {
        // The direction that could be mistaken for success: a start event that declares a timer the scope refuses
        // to register is not a plain start. Publishing it as one would leave a process that looks published and
        // whose timer never fires, so the placeholder the validator reports has to stay for this shape.
        const string processId = "main";
        var timer = new BpmnEventDefinition(
            BpmnEventDefinitionTypes.Timer,
            new Dictionary<string, string>(StringComparer.Ordinal) { [BpmnEventDefinitionProperties.Interval] = "not-an-iso-8601-duration" });
        var process = new BpmnProcess
        {
            Id = processId,
            IsRootScope = true,
            Process = new BpmnProcessBuilder(processId)
                .StartEvent("start", null, timer)
                .EndEvent("end")
                .ConnectSequence("start", "end")
                .Build()
        };

        var definitionId = await SaveDraftAsync(process);
        var result = await PublishAsync(definitionId);

        Assert.False(result.Succeeded);
        Assert.Contains(result.ValidationErrors, error => error.ActivityId == processId && error.Message == "Trigger should have a payload");
    }

    private async Task<string> ImportAsync(string assetFileName)
    {
        var imported = await DocumentService.ImportAsync(ReadAsset(assetFileName), definitionId: null, name: null, processId: null, CancellationToken.None);
        Assert.True(imported.ImportResult.Succeeded, string.Join("; ", imported.ImportResult.ValidationErrors.Select(error => error.Message)));

        return imported.ImportResult.WorkflowDefinition.DefinitionId;
    }

    private async Task<IReadOnlyCollection<StoredTrigger>> StoredTriggersAsync(string definitionId) =>
        (await Services.GetRequiredService<ITriggerStore>().FindManyAsync(new TriggerFilter { WorkflowDefinitionId = definitionId })).ToList();
}
