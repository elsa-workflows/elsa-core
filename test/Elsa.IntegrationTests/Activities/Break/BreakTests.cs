using System.Linq;
using System.Threading.Tasks;
using Elsa.Builders;
using Elsa.Contracts;
using Elsa.Testing.Shared;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace Elsa.IntegrationTests.Activities;

public class BreakWorkflowTests
{
    private readonly IWorkflowRunner _workflowRunner;
    private readonly CapturingTextWriter _capturingTextWriter = new();

    public BreakWorkflowTests(ITestOutputHelper testOutputHelper)
    {
        var services = new TestApplicationBuilder(testOutputHelper).WithCapturingTextWriter(_capturingTextWriter).Build();
        _workflowRunner = services.GetRequiredService<IWorkflowRunner>();
    }

    [Fact(DisplayName = "Break exits out of ForEach")]
    public async Task Test1()
    {
        var workflow = new WorkflowDefinitionBuilder().BuildWorkflow(new BreakForEachWorkflow());
        await _workflowRunner.RunAsync(workflow);
        var lines = _capturingTextWriter.Lines.ToList();
        Assert.Equal(new[] { "Start", "C#", "Test", "End" }, lines);
    }

    [Fact(DisplayName = "Break exits out of immediate ForEach only")]
    public async Task Test2()
    {
        var workflow = new WorkflowDefinitionBuilder().BuildWorkflow(new NestedForEachWithBreakWorkflow());
        await _workflowRunner.RunAsync(workflow);
        var lines = _capturingTextWriter.Lines.ToList();
        Assert.Equal(new[] { "C#", "Classes", "Rust", "Classes", "Go", "Classes" }, lines);
    }

    [Fact(DisplayName = "Break exits out of For")]
    public async Task Test3()
    {
        var workflow = new WorkflowDefinitionBuilder().BuildWorkflow(new BreakForWorkflow());
        await _workflowRunner.RunAsync(workflow);
        var lines = _capturingTextWriter.Lines.ToList();
        Assert.Equal(new[] { "Start", "0", "1", "End" }, lines);
    }
    
    [Fact(DisplayName = "Break exits out of While")]
    public async Task Test4()
    {
        var workflow = new WorkflowDefinitionBuilder().BuildWorkflow(new BreakWhileWorkflow());
        await _workflowRunner.RunAsync(workflow);
        var lines = _capturingTextWriter.Lines.ToList();
        Assert.Equal(new[] { "Start", "1", "2", "End" }, lines);
    }
}