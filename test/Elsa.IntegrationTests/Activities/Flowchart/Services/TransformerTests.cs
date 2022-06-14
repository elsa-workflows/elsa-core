using System.Linq;
using System.Threading.Tasks;
using Elsa.Testing.Shared;
using Elsa.Workflows.Core.Activities.Flowchart.Services;
using Elsa.Workflows.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace Elsa.IntegrationTests.Activities.Flowchart.Services;

using Flowchart = Workflows.Core.Activities.Flowchart.Activities.Flowchart;

public class TransformerTests
{
    private readonly ITransformer _transformer;
    private readonly CapturingTextWriter _capturingTextWriter = new();
    private readonly IWorkflowRunner _workflowRunner;
    private readonly IWorkflowDefinitionBuilderFactory _workflowBuilderFactory;

    public TransformerTests(ITestOutputHelper testOutputHelper)
    {
        var services = new TestApplicationBuilder(testOutputHelper).WithCapturingTextWriter(_capturingTextWriter).Build();
        _transformer = services.GetRequiredService<ITransformer>();
        _workflowBuilderFactory = services.GetRequiredService<IWorkflowDefinitionBuilderFactory>();
        _workflowRunner = services.GetRequiredService<IWorkflowRunner>();
    }

    [Fact(DisplayName = "Transforming a flowchart correctly transposes connected nodes")]
    public async Task Test1()
    {
        var languages = new[] { "C#", "Rust", "Go" };
        var builder = _workflowBuilderFactory.CreateBuilder();
        var workflow = await builder.BuildWorkflowAsync(new Workflow1(languages));
        var flowchart = (Flowchart)workflow.Root;
        _transformer.Transpose(flowchart);
        await _workflowRunner.RunAsync(workflow);
        var lines = _capturingTextWriter.Lines.ToList();
        var expectedLines = new[] { "Start!", "Current Item", "C#", "Current Item", "Rust", "Current Item", "Go", "Done!" };

        Assert.Equal(expectedLines, lines);
    }
    
    [Fact(DisplayName = "Transforming a flowchart correctly transposes connected nodes")]
    public async Task Test2()
    {
        var languages = new[] { "C#", "Rust", "Go" };
        var builder = _workflowBuilderFactory.CreateBuilder();
        var workflow = await builder.BuildWorkflowAsync(new Workflow2(languages));
        var flowchart = (Flowchart)workflow.Root;
        _transformer.Transpose(flowchart);
        await _workflowRunner.RunAsync(workflow);
        var lines = _capturingTextWriter.Lines.ToList();
        var expectedLines = new[] { "Start!", "C#", "Rust", "Go", "Done!" };

        Assert.Equal(expectedLines, lines);
    }
    
    [Fact(DisplayName = "Transforming a flowchart correctly transposes connected nodes")]
    public async Task Test3()
    {
        var languages = new[] { "C#", "Rust", "Go" };
        var builder = _workflowBuilderFactory.CreateBuilder();
        var workflow = await builder.BuildWorkflowAsync(new Workflow3(languages));
        var flowchart = (Flowchart)workflow.Root;
        _transformer.Transpose(flowchart);
        await _workflowRunner.RunAsync(workflow);
        var lines = _capturingTextWriter.Lines.ToList();
        var expectedLines = new[] { "Start!", "C#", "Rust", "Go", "Done!" };

        Assert.Equal(expectedLines, lines);
    }
}