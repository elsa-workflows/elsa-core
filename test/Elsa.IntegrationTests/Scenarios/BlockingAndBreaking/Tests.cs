using System.Linq;
using System.Threading.Tasks;
using Elsa.Builders;
using Elsa.Contracts;
using Elsa.Testing.Shared;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace Elsa.IntegrationTests.Scenarios.BlockingAndBreaking;

public class Tests
{
    private readonly IWorkflowRunner _workflowRunner;
    private readonly CapturingTextWriter _capturingTextWriter = new();

    public Tests(ITestOutputHelper testOutputHelper)
    {
        var services = new TestApplicationBuilder(testOutputHelper).WithCapturingTextWriter(_capturingTextWriter).Build();
        _workflowRunner = services.GetRequiredService<IWorkflowRunner>();
    }

    [Fact(DisplayName = "While loop blocks when bookmark is created")]
    public async Task Test1()
    {
        var workflow = new WorkflowDefinitionBuilder().BuildWorkflow<BreakWhileBlockForkWorkflow>();
        
        // Start workflow.
        await _workflowRunner.RunAsync(workflow);

        // Verify expected output.
        var lines = _capturingTextWriter.Lines.ToList();
        Assert.Equal(new[] { "Current value: 0", "Branch 1", "Blocking Branch 2" }, lines);
    }
}