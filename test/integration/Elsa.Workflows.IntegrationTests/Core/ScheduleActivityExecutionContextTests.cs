using Elsa.Testing.Shared;
using Elsa.Workflows.Activities;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.IntegrationTests.Core;

public class ScheduleActivityExecutionContextTests : IAsyncDisposable
{
    private readonly IServiceProvider _serviceProvider = new TestApplicationBuilder(TestContext.Current!.Output.StandardOutput).Build();

    [Test]
    [DisplayName("Scheduling an activity that is not part of the workflow should throw an exception")]
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
        var workflowExecutionContext = await WorkflowExecutionContext.CreateAsync(_serviceProvider, workflowGraph, "test", CancellationToken.None);
        var activityExecutionContext = await workflowExecutionContext.CreateActivityExecutionContextAsync(writeLineA);

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => activityExecutionContext.ScheduleActivityAsync(writeLineB).AsTask());
    }

    public ValueTask DisposeAsync() => TestResourceDisposal.DisposeAsync(_serviceProvider);
}
