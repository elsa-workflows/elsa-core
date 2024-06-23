using Elsa.Testing.Shared;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace Elsa.Workflows.IntegrationTests.Core;

public class ScheduleActivityExecutionContextTests(ITestOutputHelper testOutputHelper)
{
    private readonly IServiceProvider _serviceProvider = new TestApplicationBuilder(testOutputHelper).Build();

    [Fact(DisplayName = "Scheduling an activity that is not part of the workflow should throw an exception")]
    public async Task ScheduleActivityAsync_WithActivityNotPartOfWorkflow_ShouldThrowException()
    {
        await _serviceProvider.PopulateRegistriesAsync();
        var writeLineA = new WriteLine("Test");
        var writeLineB = new WriteLine("Test");
        var workflow = new Workflow
        {
            Root = writeLineA
        };
        var workflowGraphBuilder = _serviceProvider.GetRequiredService<IWorkflowGraphBuilder>();
        var workflowGraph = await workflowGraphBuilder.BuildAsync(workflow);
        var workflowExecutionContext = await WorkflowExecutionContext.CreateAsync(_serviceProvider, workflowGraph, "test");
        var activityExecutionContext = await workflowExecutionContext.CreateActivityExecutionContextAsync(writeLineA);

        await Assert.ThrowsAsync<InvalidOperationException>(() => activityExecutionContext.ScheduleActivityAsync(writeLineB).AsTask());
    }
}