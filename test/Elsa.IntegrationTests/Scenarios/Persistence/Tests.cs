using System.Threading.Tasks;
using Elsa.Extensions;
using Elsa.Persistence.Common.Implementations;
using Elsa.Testing.Shared;
using Elsa.Workflows.Core;
using Elsa.Workflows.Core.Builders;
using Elsa.Workflows.Core.Pipelines.WorkflowExecution.Components;
using Elsa.Workflows.Core.Services;
using Elsa.Workflows.Persistence.Entities;
using Elsa.Workflows.Runtime.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace Elsa.IntegrationTests.Scenarios.Persistence;

public class WorkflowInstancePersistenceTests
{
    private readonly IWorkflowRunner _workflowRunner;
    private readonly CapturingTextWriter _capturingTextWriter = new();
    private readonly MemoryStore<WorkflowInstance> _workflowInstanceStore;
    private readonly MemoryStore<WorkflowBookmark> _workflowBookmarkStore;

    public WorkflowInstancePersistenceTests(ITestOutputHelper testOutputHelper)
    {
        var services = new TestApplicationBuilder(testOutputHelper).WithCapturingTextWriter(_capturingTextWriter).Build();
        _workflowRunner = services.GetRequiredService<IWorkflowRunner>();
        _workflowInstanceStore = services.GetRequiredService<MemoryStore<WorkflowInstance>>();
        _workflowBookmarkStore = services.GetRequiredService<MemoryStore<WorkflowBookmark>>();
        
        services.ConfigureDefaultWorkflowExecutionPipeline(pipeline => pipeline
            .UsePersistence()
            .UseStackBasedActivityScheduler());
    }

    [Fact(DisplayName = "Executing a workflow creates a workflow instance")]
    public async Task Test1()
    {
        var workflow = await new WorkflowDefinitionBuilder().BuildWorkflowAsync<SequentialWorkflow>();
        var result = await _workflowRunner.RunAsync(workflow);
        var workflowInstance = _workflowInstanceStore.Find(x => x.Id == result.WorkflowState.Id);

        Assert.NotNull(workflowInstance);
    }
    
    [Fact(DisplayName = "Bookmarks are persisted")]
    public async Task Test2()
    {
        var workflow = await new WorkflowDefinitionBuilder().BuildWorkflowAsync<SequentialWorkflow>();
        await _workflowRunner.RunAsync(workflow);
        var workflowBookmark = _workflowBookmarkStore.Find(x => x.ActivityId == "Resume");

        Assert.NotNull(workflowBookmark);
    }
}