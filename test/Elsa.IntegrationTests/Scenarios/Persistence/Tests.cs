using System.Threading.Tasks;
using Elsa.Common.Implementations;
using Elsa.Extensions;
using Elsa.Testing.Shared;
using Elsa.Workflows.Core.Pipelines.WorkflowExecution.Components;
using Elsa.Workflows.Core.Services;
using Elsa.Workflows.Management.Entities;
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

    public WorkflowInstancePersistenceTests(ITestOutputHelper testOutputHelper)
    {
        var services = new TestApplicationBuilder(testOutputHelper).WithCapturingTextWriter(_capturingTextWriter).Build();
        _workflowRunner = services.GetRequiredService<IWorkflowRunner>();
        _workflowInstanceStore = services.GetRequiredService<MemoryStore<WorkflowInstance>>();

        services.ConfigureDefaultWorkflowExecutionPipeline(pipeline => pipeline
            .UsePersistentVariables()
            .UseStackBasedActivityScheduler());
    }

    [Fact(DisplayName = "Executing a workflow creates a workflow instance")]
    public async Task Test1()
    {
        var result = await _workflowRunner.RunAsync<SequentialWorkflow>();
        var workflowInstance = _workflowInstanceStore.Find(x => x.Id == result.WorkflowState.Id);

        Assert.NotNull(workflowInstance);
    }
}