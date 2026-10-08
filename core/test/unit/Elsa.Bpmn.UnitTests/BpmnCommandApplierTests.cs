using System.Collections.Generic;
using Bpmn.Semantics;
using Elsa.Bpmn.Activities;
using Elsa.Bpmn.Hosting;
using Elsa.Testing.Shared;
using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Bpmn.UnitTests;

/// <summary>
/// Covers the root-position refusal in <see cref="BpmnCommandApplier.ApplyAsync"/>: a <c>StartWork</c> command bound
/// to a nested <see cref="BpmnProcess"/> that (mis)declares itself the workflow's root scope must be refused before
/// any command in the same batch is applied — not mid-list, after earlier commands already mutated and saved scope
/// memory. Under <c>ContinueWithIncidentsStrategy</c> the throw is absorbed into an incident rather than surfaced, so
/// a partial application would otherwise leave the scope silently half-mutated.
/// </summary>
public class BpmnCommandApplierTests
{
    private const string OrdinaryActivityId = "ordinary-activity";
    private const string NestedProcessActivityId = "nested-activity";

    [Fact]
    public async Task ApplyAsync_RefusesTheWholeBatch_WhenALaterCommandBindsARootScopeProcess()
    {
        var (scopeContext, process) = await BuildScopeAsync();
        var memory = BpmnScopeMemory.Load(scopeContext);
        var applier = new BpmnCommandApplier(scopeContext, process, memory);

        var commands = new BpmnHostCommand[]
        {
            new BpmnHostCommand.StartWork("ordinary-binding", "ordinary-element", "token-1", "cause", new Dictionary<string, string>(), null, null),
            new BpmnHostCommand.StartWork("nested-binding", "nested-element", "token-2", "cause", new Dictionary<string, string>(), null, null)
        };

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => applier.ApplyAsync(commands).AsTask());

        Assert.Contains(NestedProcessActivityId, exception.Message);

        // The earlier command in the same batch — the ordinary StartWork — must not have been applied: no work
        // record persisted, and no child context created for it.
        var reloaded = BpmnScopeMemory.Load(scopeContext);
        Assert.Empty(reloaded.Work.Records);
        Assert.DoesNotContain(scopeContext.WorkflowExecutionContext.ActivityExecutionContexts, context => context.Activity.Id == OrdinaryActivityId);
    }

    private static async Task<(ActivityExecutionContext ScopeContext, BpmnProcess Process)> BuildScopeAsync()
    {
        var ordinaryActivity = new WriteLine("ordinary") { Id = OrdinaryActivityId };
        var nestedProcess = new BpmnProcess { Id = NestedProcessActivityId, IsRootScope = true };
        var process = new BpmnProcess
        {
            Id = "process-activity",
            Activities = { ordinaryActivity, nestedProcess },
            WorkBindings = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["ordinary-binding"] = OrdinaryActivityId,
                ["nested-binding"] = NestedProcessActivityId
            }
        };

        var fixture = new ActivityTestFixture(process);
        fixture.ConfigureServices(services => services.AddSingleton<IIdentityGenerator, GuidIdentityGenerator>());

        var scopeContext = await fixture.BuildAsync();
        var workflowExecutionContext = scopeContext.WorkflowExecutionContext;

        await workflowExecutionContext.ActivityRegistry.RegisterAsync(typeof(WriteLine));
        await workflowExecutionContext.ActivityRegistry.RegisterAsync(typeof(BpmnProcess));

        return (scopeContext, process);
    }
}
