using System.Threading.Tasks;
using Elsa.Builders;
using Elsa.Extensions;
using Elsa.Persistence.Entities;
using Elsa.Persistence.InMemory.Implementations;
using Elsa.Pipelines.WorkflowExecution.Components;
using Elsa.Runtime.Extensions;
using Elsa.Services;
using Elsa.Testing.Shared;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace Elsa.IntegrationTests.Scenarios.Persistence;

public class WorkflowInstancePersistenceTests
{
    private readonly IWorkflowRunner _workflowRunner;
    private readonly CapturingTextWriter _capturingTextWriter = new();
    private readonly InMemoryStore<WorkflowInstance> _workflowInstanceStore;
    private readonly InMemoryStore<WorkflowBookmark> _workflowBookmarkStore;

    public WorkflowInstancePersistenceTests(ITestOutputHelper testOutputHelper)
    {
        var services = new TestApplicationBuilder(testOutputHelper).WithCapturingTextWriter(_capturingTextWriter).Build();
        _workflowRunner = services.GetRequiredService<IWorkflowRunner>();
        _workflowInstanceStore = services.GetRequiredService<InMemoryStore<WorkflowInstance>>();
        _workflowBookmarkStore = services.GetRequiredService<InMemoryStore<WorkflowBookmark>>();
        
        services.ConfigureDefaultWorkflowExecutionPipeline(pipeline => pipeline
            .UsePersistence()
            .UseStackBasedActivityScheduler());
    }

    [Fact(DisplayName = "Executing a workflow creates a workflow instance")]
    public async Task Test1()
    {
        var workflow = new WorkflowDefinitionBuilder().BuildWorkflow<BlockingSequentialWorkflow>();
        var result = await _workflowRunner.RunAsync(workflow);
        var workflowInstance = _workflowInstanceStore.Find(x => x.Id == result.WorkflowState.Id);

        Assert.NotNull(workflowInstance);
    }
    
    [Fact(DisplayName = "Bookmarks are persisted")]
    public async Task Test2()
    {
        var workflow = new WorkflowDefinitionBuilder().BuildWorkflow<BlockingSequentialWorkflow>();
        await _workflowRunner.RunAsync(workflow);
        var workflowBookmark = _workflowBookmarkStore.Find(x => x.ActivityId == "Resume");

        Assert.NotNull(workflowBookmark);
    }
}