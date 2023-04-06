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

public class BraidedWorkflowTests
{
    private readonly IWorkflowRunner _workflowRunner;
    private readonly CapturingTextWriter _capturingTextWriter = new();

    public BraidedWorkflowTests(ITestOutputHelper testOutputHelper)
    {
        var services = new TestApplicationBuilder(testOutputHelper).WithCapturingTextWriter(_capturingTextWriter).Build();
        _workflowRunner = services.GetRequiredService<IWorkflowRunner>();
    }

    [Fact(DisplayName = "Braided workflows are executed correctly")]
    public async Task Test1()
    {
        await _workflowRunner.RunAsync<BraidedWorkflow>();
        var lines = _capturingTextWriter.Lines.ToList();
        Assert.Equal(new[] { "WriteLine1", "WriteLine2", "WriteLine3", "WriteLine4", "WriteLine5", "WriteLine6", "WriteLine7" }, lines);
    }
    
    [Fact(DisplayName = "Braided workflows complete the workflow")]
    public async Task Test2()
    {
        var result = await _workflowRunner.RunAsync<BraidedWorkflow>();
        Assert.Equal(WorkflowStatus.Finished, result.WorkflowState.Status);
    }
}