using Bpmn.Interchange;
using Bpmn.Model;
using Elsa.Bpmn.Activities;
using Elsa.Bpmn.Interchange.Binding;
using Elsa.Common.Models;
using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Xunit.Abstractions;

namespace Elsa.Bpmn.Interchange.IntegrationTests.Scenarios.Publishing;

/// <summary>
/// The publish-time second net for <see cref="BpmnWorkBinding.UnboundTask"/> (#7931/W8): <see cref="Binding.BpmnWorkBinder"/>
/// already refuses one at import, naming the element; these tests are for the definition that got past that first
/// net and now needs the second one — one that imported clean and was edited afterward.
/// </summary>
public class BpmnPublishGateTests(ITestOutputHelper testOutputHelper) : BpmnPublishGateTestBase(testOutputHelper)
{
    private const string ProcessId = "main";

    [Fact(DisplayName = "A process whose unbound task has no bound activity fails publication, naming the element")]
    public async Task UnboundTaskWithNoBoundActivity_FailsPublication()
    {
        // Built by hand rather than through BpmnWorkBinder: the binder itself already refuses this shape at bind
        // time (that is the first net, unchanged by this issue), so the only way to reach a materialized workflow
        // this second net has to catch is to construct the graph directly - exactly what a post-import edit that
        // deletes a bound activity, or a hand-authored workflow, would leave behind.
        // IsRootScope is left at its default (false): root position only affects trigger registration, which this
        // fixture declares no start event for and which the binding gate under test does not consult.
        var process = new BpmnProcess
        {
            Id = ProcessId,
            Process = new BpmnProcessDefinition(ProcessId, Elements: [new BpmnElement("ServiceTask1", BpmnElementTypes.ServiceTask, bindingRef: "ref-ServiceTask1")])
        };

        var definitionId = await SaveDraftAsync(process);
        var result = await PublishAsync(definitionId);

        Assert.False(result.Succeeded);
        // ActivityId names the containing BpmnProcess node - the real node in the materialized graph - not the BPMN
        // element id, which resolves to no node there; the element id still appears in the message text.
        Assert.Contains(result.ValidationErrors, error => error.ActivityId == process.Id && error.Message.Contains("ServiceTask1"));
    }

    [Fact(DisplayName = "A process whose task is bound to a no-longer-installed activity type fails publication, naming the element and the missing type")]
    public async Task UnboundTaskWithNotFoundActivity_FailsPublication()
    {
        // Simulates the module-removed scenario the finding describes without needing to actually uninstall a
        // module: a NotFoundActivity is exactly what IActivitySerializer hands back when a binding's activity type
        // can no longer be resolved (see BpmnActivityBindingFormat, which refuses this at bind time for the same
        // reason), so constructing one directly reproduces the shape a stale WorkBindings entry leaves behind.
        const string activityId = "notfound-1";
        const string missingTypeName = "Acme.RemovedActivity";
        var process = new BpmnProcess
        {
            Id = ProcessId,
            Process = new BpmnProcessDefinition(ProcessId, Elements: [new BpmnElement("ServiceTask1", BpmnElementTypes.ServiceTask, bindingRef: "ref-ServiceTask1")]),
            WorkBindings = new Dictionary<string, string>(StringComparer.Ordinal) { ["ref-ServiceTask1"] = activityId },
            Activities =
            [
                new NotFoundActivity(missingTypeName: missingTypeName)
                {
                    Id = activityId,
                    // Round-tripped through the JSON activity serializer on save and on publish: ActivityJsonConverter
                    // re-derives a NotFoundActivity's missing-type identity from this, not from the in-memory
                    // properties set above, so it needs to look like what that converter itself would have written.
                    OriginalActivityJson = $$"""{"id":"{{activityId}}","type":"{{missingTypeName}}","version":1}"""
                }
            ]
        };

        var definitionId = await SaveDraftAsync(process);
        var result = await PublishAsync(definitionId);

        Assert.False(result.Succeeded);
        Assert.Contains(result.ValidationErrors, error =>
            error.ActivityId == process.Id
            && error.Message.Contains("ServiceTask1")
            && error.Message.Contains("no longer installed")
            && error.Message.Contains(missingTypeName));
    }

    [Fact(DisplayName = "A BPMN process composed inside a Flowchart with an unbound task fails publication, naming the element")]
    public async Task UnboundTaskInsideFlowchartComposedProcess_FailsPublication()
    {
        // Proves the justification for reaching the graph through IWorkflowGraphBuilder rather than a hand-rolled
        // walk of Container.Activities (see this handler's own remarks): a BpmnProcess nested a level deeper, inside
        // a Flowchart, must still be found and checked.
        var process = new BpmnProcess
        {
            Id = ProcessId,
            Process = new BpmnProcessDefinition(ProcessId, Elements: [new BpmnElement("ServiceTask1", BpmnElementTypes.ServiceTask, bindingRef: "ref-ServiceTask1")])
        };
        var flowchart = new Elsa.Workflows.Activities.Flowchart.Activities.Flowchart { Activities = [process] };

        var definitionId = await SaveDraftAsync(flowchart);
        var result = await PublishAsync(definitionId);

        Assert.False(result.Succeeded);
        Assert.Contains(result.ValidationErrors, error => error.ActivityId == process.Id && error.Message.Contains("ServiceTask1"));
    }

    [Fact(DisplayName = "A process whose tasks are all bound publishes successfully")]
    public async Task AllTasksBound_PublishesSuccessfully()
    {
        // Proves the gate is not satisfied by refusing everything: a fully bound scope, produced by the same
        // binder the first net uses, must not trip it.
        var scope = Binder.Bind(
            new BpmnProcessDefinition(ProcessId, Elements: [BoundElement("approve", BpmnElementTypes.UserTask, new WriteLine("approved"))]),
            [new BpmnWorkBinding.UnboundTask(ProcessId, "approve", Ref("approve"), BpmnBindingSlot.Primary, BpmnElementTypes.UserTask)]);

        // Bind always marks the scope it returns as root position (see its own remarks); this fixture declares no
        // start event; clearing it keeps the unrelated "a startable trigger needs a payload" runtime validation out
        // of what is being asserted here.
        scope.IsRootScope = false;

        var definitionId = await SaveDraftAsync(scope);
        var result = await PublishAsync(definitionId);

        Assert.True(result.Succeeded, string.Join("; ", result.ValidationErrors.Select(error => error.Message)));
    }

    [Fact(DisplayName = "A definition that imported cleanly and then had its binding removed fails publication on re-publish")]
    public async Task DefinitionEditedAfterImport_LosesItsBinding_FailsRepublication()
    {
        var xml = ReadAsset("publish-gate-process.bpmn");
        var imported = await DocumentService.ImportAsync(xml, definitionId: null, name: null, processId: null, CancellationToken.None);
        Assert.True(imported.ImportResult.Succeeded, string.Join("; ", imported.ImportResult.ValidationErrors.Select(error => error.Message)));

        var definitionId = imported.ImportResult.WorkflowDefinition.DefinitionId;

        // Publishing the document as imported succeeds - the binding is intact, so this is not a gate that refuses
        // everything either.
        var publishedAsImported = await PublishAsync(definitionId);
        Assert.True(publishedAsImported.Succeeded, string.Join("; ", publishedAsImported.ValidationErrors.Select(error => error.Message)));

        // Simulate the edit the issue exists for: through Elsa's own designer, not the BPMN document, the activity
        // NotifyWarehouse was bound to gets deleted from the graph. The BPMN source this scope still carries -
        // Process.Elements, and its elsa:activityBinding - is untouched by this; only WorkBindings/Activities move.
        var definition = await DefinitionService.FindWorkflowDefinitionAsync(definitionId, VersionOptions.Latest);
        var graph = await DefinitionService.MaterializeWorkflowAsync(definition!);
        var scope = Assert.IsType<BpmnProcess>(graph.Workflow.Root);
        var boundActivityId = Assert.Single(scope.WorkBindings.Values);
        var boundActivity = Assert.Single(scope.Activities, activity => activity.Id == boundActivityId);

        Assert.True(scope.Activities.Remove(boundActivity));

        await SaveDraftAsync(scope, definitionId);
        var publishedAfterEdit = await PublishAsync(definitionId);

        Assert.False(publishedAfterEdit.Succeeded);
        // Same distinction as above: ActivityId is the containing BpmnProcess's node id, not the removed element's.
        Assert.Contains(publishedAfterEdit.ValidationErrors, error => error.ActivityId == scope.Id && error.Message.Contains("NotifyWarehouse"));
    }
}
