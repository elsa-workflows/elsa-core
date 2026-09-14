using Elsa.Expressions.Helpers;
using Elsa.Extensions;
using Elsa.Workflows.ComponentTests.Abstractions;
using Elsa.Workflows.ComponentTests.Fixtures;
using Elsa.Workflows.Management;
using Elsa.Workflows.Models;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Messages;
using Elsa.Workflows.State;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.ComponentTests.Scenarios.JavaScriptVariables;

public class JavaScriptVariablesWorkflowTests(App app) : AppComponentTest(app)
{
    [Test]
    [DisplayName("SetVariable JS function sets a variable and does not get overridden by variables API (workflow: $workflowName)")]
    [MethodDataSource(nameof(GetWorkflowDefinitions))]
    public async Task SetVariableRetainsValue(string workflowName, string workflowDefinitionId)
    {
        var workflowRuntime = Scope.ServiceProvider.GetRequiredService<IWorkflowRuntime>();
        var workflowInstanceStore = Scope.ServiceProvider.GetRequiredService<IWorkflowInstanceStore>();
        var workflowClient = await workflowRuntime.CreateClientAsync();
        var runAndCreateRequest = new CreateAndRunWorkflowInstanceRequest
        {
            WorkflowDefinitionHandle = WorkflowDefinitionHandle.ByDefinitionId(workflowDefinitionId)
        };
        var runResponse = await workflowClient.CreateAndRunInstanceAsync(runAndCreateRequest);
        var workflowInstanceId = runResponse.WorkflowInstanceId;
        var workflowInstance = await workflowInstanceStore.FindAsync(workflowInstanceId);
        var workflowState = workflowInstance!.WorkflowState;
        var rootWorkflowActivityExecutionContext = workflowState.ActivityExecutionContexts.Single(x => x.ParentContextId == null);
        var variables = GetVariablesDictionary(rootWorkflowActivityExecutionContext);
        var magicNumber = variables["magicNumberVariable"].ConvertTo<int>();
        await Assert.That(magicNumber).IsEqualTo(42);
    }

    public static IEnumerable<(string workflowName, string workflowDefinitionId)> GetWorkflowDefinitions()
    {
        return
        [
            (nameof(JavaScriptVariablesWorkflow1), JavaScriptVariablesWorkflow1.DefinitionId),
            (nameof(JavaScriptVariablesWorkflow2), JavaScriptVariablesWorkflow2.DefinitionId),
            (nameof(JavaScriptVariablesWorkflow3), JavaScriptVariablesWorkflow3.DefinitionId)
        ];
    }

    private VariablesDictionary GetVariablesDictionary(ActivityExecutionContextState context)
    {
        return context.Properties.GetOrAdd(WorkflowInstanceStorageDriver.VariablesDictionaryStateKey, () => new VariablesDictionary());
    }
}
