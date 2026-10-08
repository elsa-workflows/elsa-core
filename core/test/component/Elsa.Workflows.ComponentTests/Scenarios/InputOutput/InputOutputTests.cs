using Elsa.Workflows.ComponentTests.Abstractions;
using Elsa.Workflows.ComponentTests.Fixtures;
using Elsa.Workflows.Management;
using Elsa.Workflows.Runtime;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.ComponentTests.Scenarios.InputOutput;

public class InputOutputTests(App app) : AppComponentTest(app)
{
    [Fact(DisplayName = "Input should be received by the Provider and should in turn be able to send it back to the Consumer.")]
    public async Task InputShouldBeOutput()
    {
        const string workflowDefinitionVersionId = "4e993ab1b3d4e4a1";
        var workflowDefinitionService = Scope.ServiceProvider.GetRequiredService<IWorkflowDefinitionService>();
        var workflowInvoker = Scope.ServiceProvider.GetRequiredService<IWorkflowInvoker>();
        var variableManager = Scope.ServiceProvider.GetRequiredService<IWorkflowInstanceVariableManager>();
        var workflowGraph = await workflowDefinitionService.FindWorkflowGraphAsync(workflowDefinitionVersionId);
        var result = await workflowInvoker.InvokeAsync(workflowGraph!);
        var variables = await variableManager.GetVariablesAsync(result.WorkflowState.Id);
        var variableValue = variables.First().Value;
        
        Assert.Equal("Hello World!", variableValue);
    }
}