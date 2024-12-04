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
    [Theory(DisplayName = "SetVariable JS function sets a variable and does not get overridden by variables API")]
    [MemberData(nameof(GetWorkflowDefinitions))]
    public async Task SetVariableRetainsValue(string workflowDefinitionId)
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
        var magicNumber = variables["Workflow1:variable-1"].ConvertTo<int>();
        Assert.Equal(42, magicNumber);
    }

    public static IEnumerable<object[]> GetWorkflowDefinitions()
    {
        return
        [
            [JavaScriptVariablesWorkflow1.DefinitionId],
            [JavaScriptVariablesWorkflow2.DefinitionId],
            [JavaScriptVariablesWorkflow3.DefinitionId]
        ];
    }

    private VariablesDictionary GetVariablesDictionary(ActivityExecutionContextState context)
    {
        return context.Properties.GetOrAdd(WorkflowInstanceStorageDriver.VariablesDictionaryStateKey, () => new VariablesDictionary());
    }
}