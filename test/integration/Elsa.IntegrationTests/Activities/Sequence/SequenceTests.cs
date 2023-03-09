using System.Linq;
using System.Threading.Tasks;
using Elsa.Testing.Shared;
using Elsa.Workflows.Core.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace Elsa.IntegrationTests.Activities;

public class SequenceTests
{
    private readonly IWorkflowRunner _workflowRunner;
    private readonly CapturingTextWriter _capturingTextWriter = new();

    public SequenceTests(ITestOutputHelper testOutputHelper)
    {
        var services = new TestApplicationBuilder(testOutputHelper).WithCapturingTextWriter(_capturingTextWriter).Build();
        _workflowRunner = services.GetRequiredService<IWorkflowRunner>();
    }
    
    [Fact(DisplayName = "Sequence completes after its child activities complete")]
    public async Task Test1()
    {
        await _workflowRunner.RunAsync(new SequentialWorkflow());
        var lines = _capturingTextWriter.Lines.ToList();
        Assert.Equal(new[] { "Line 1", "Line 2", "Line 3" }, lines);
    }

    [Fact(DisplayName = "Sequence completes after its child sequence activity complete")]
    public async Task Test2()
    {
        await _workflowRunner.RunAsync(new NestedSequentialWorkflow());
        var lines = _capturingTextWriter.Lines.ToList();
        Assert.Equal(new[] { "Start", "Line 1", "Line 2", "Line 3", "End" }, lines);
    }
    

}