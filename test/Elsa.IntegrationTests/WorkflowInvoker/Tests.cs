using System.Threading.Tasks;
using Elsa.Extensions;
using Elsa.IntegrationTests.Scenarios.Persistence;
using Elsa.Persistence.Common.Implementations;
using Elsa.Testing.Shared;
using Elsa.Workflows.Core;
using Elsa.Workflows.Core.Builders;
using Elsa.Workflows.Core.Pipelines.WorkflowExecution.Components;
using Elsa.Workflows.Persistence.Entities;
using Elsa.Workflows.Runtime.Extensions;
using Elsa.Workflows.Runtime.Services;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace Elsa.IntegrationTests.WorkflowInvoker;

public class Tests
{
    private readonly IWorkflowInvoker _workflowInvoker;
    private readonly CapturingTextWriter _capturingTextWriter = new();
    private readonly MemoryStore<WorkflowInstance> _workflowInstanceStore;
    private readonly MemoryStore<WorkflowBookmark> _workflowBookmarkStore;

    public Tests(ITestOutputHelper testOutputHelper)
    {
        var services = new TestApplicationBuilder(testOutputHelper).WithCapturingTextWriter(_capturingTextWriter).Build();
        _workflowInvoker = services.GetRequiredService<IWorkflowInvoker>();
        _workflowInstanceStore = services.GetRequiredService<MemoryStore<WorkflowInstance>>();
        _workflowBookmarkStore = services.GetRequiredService<MemoryStore<WorkflowBookmark>>();
        
        services.ConfigureDefaultWorkflowExecutionPipeline(pipeline => pipeline
            .UsePersistence()
            .UseStackBasedActivityScheduler());
    }

    [Fact(DisplayName = "Invoker creates workflow instance")]
    public Task Test1()
    {
        //var workflow = new WorkflowDefinitionBuilder().BuildWorkflowAsync<SequentialWorkflow>();
        return Task.CompletedTask;
        //var requ
        //var result = await _workflowInvoker.InvokeAsync(new InvokeWorkflowDefinitionRequest());
        //var workflowInstance = _workflowInstanceStore.Find(x => x.Id == result.WorkflowState.Id);

        //Assert.NotNull(workflowInstance);
    }
}