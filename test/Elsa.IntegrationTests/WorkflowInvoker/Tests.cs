using System.Threading.Tasks;
using Elsa.Builders;
using Elsa.Extensions;
using Elsa.IntegrationTests.Scenarios.Persistence;
using Elsa.Persistence.Entities;
using Elsa.Persistence.InMemory.Implementations;
using Elsa.Pipelines.WorkflowExecution.Components;
using Elsa.Runtime.Extensions;
using Elsa.Runtime.Services;
using Elsa.Testing.Shared;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace Elsa.IntegrationTests.WorkflowInvoker;

public class Tests
{
    private readonly IWorkflowInvoker _workflowInvoker;
    private readonly CapturingTextWriter _capturingTextWriter = new();
    private readonly InMemoryStore<WorkflowInstance> _workflowInstanceStore;
    private readonly InMemoryStore<WorkflowBookmark> _workflowBookmarkStore;

    public Tests(ITestOutputHelper testOutputHelper)
    {
        var services = new TestApplicationBuilder(testOutputHelper).WithCapturingTextWriter(_capturingTextWriter).Build();
        _workflowInvoker = services.GetRequiredService<IWorkflowInvoker>();
        _workflowInstanceStore = services.GetRequiredService<InMemoryStore<WorkflowInstance>>();
        _workflowBookmarkStore = services.GetRequiredService<InMemoryStore<WorkflowBookmark>>();
        
        services.ConfigureDefaultWorkflowExecutionPipeline(pipeline => pipeline
            .UsePersistence()
            .UseStackBasedActivityScheduler());
    }

    [Fact(DisplayName = "Invoker creates workflow instance")]
    public async Task Test1()
    {
        var workflow = new WorkflowDefinitionBuilder().BuildWorkflowAsync<SequentialWorkflow>();
        //var requ
        //var result = await _workflowInvoker.InvokeAsync(new InvokeWorkflowDefinitionRequest());
        //var workflowInstance = _workflowInstanceStore.Find(x => x.Id == result.WorkflowState.Id);

        //Assert.NotNull(workflowInstance);
    }
}