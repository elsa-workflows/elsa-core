using System.Linq;
using System.Threading.Tasks;
using Elsa.IntegrationTests.Scenarios.ImplicitJoins.Workflows;
using Elsa.Testing.Shared;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Models;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace Elsa.IntegrationTests.Scenarios.ImplicitJoins;

public class ImplicitWorkflowTests
{
    private readonly IWorkflowRunner _workflowRunner;
    private readonly CapturingTextWriter _capturingTextWriter = new();

    public ImplicitWorkflowTests(ITestOutputHelper testOutputHelper)
    {
        var services = new TestApplicationBuilder(testOutputHelper).WithCapturingTextWriter(_capturingTextWriter).Build();
        _workflowRunner = services.GetRequiredService<IWorkflowRunner>();
    }

    [Fact(DisplayName = "Implicit loop workflows are executed correctly")]
    public async Task Test1()
    {
        await _workflowRunner.RunAsync<ImplicitLoopWorkflow>();
        var lines = _capturingTextWriter.Lines.ToList();
        Assert.Equal(new[] { "Start", "Retry", "End" }, lines);
    }
    
    [Fact(DisplayName = "Implicit loop workflows complete the workflow")]
    public async Task Test2()
    {
        var result = await _workflowRunner.RunAsync<ImplicitLoopWorkflow>();
        Assert.Equal(WorkflowStatus.Finished, result.WorkflowState.Status);
    }
}