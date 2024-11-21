using Elsa.Workflows.ComponentTests.Scenarios.ExecuteWorkflows.Workflows;
using Elsa.Workflows.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.ComponentTests.Scenarios.ExecuteWorkflows;

public class ExecuteWorkflowsTests : AppComponentTest
{
    private readonly IWorkflowRunner _workflowRunner;

    public ExecuteWorkflowsTests(App app) : base(app)
    {
        _workflowRunner = Scope.ServiceProvider.GetRequiredService<IWorkflowRunner>();
    }

    [Fact]
    public async Task ExecuteWorkflow_ShouldExecuteWorkflow()
    {
        await _workflowRunner.RunAsync<MainWorkflow>();
    }
}