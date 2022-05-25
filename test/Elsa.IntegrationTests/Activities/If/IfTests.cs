using System.Linq;
using System.Threading.Tasks;
using Elsa.Testing.Shared;
using Elsa.Workflows.Core.Builders;
using Elsa.Workflows.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace Elsa.IntegrationTests.Activities;

public class IfTests
{
    private readonly IWorkflowRunner _workflowRunner;
    private readonly CapturingTextWriter _capturingTextWriter = new();

    public IfTests(ITestOutputHelper testOutputHelper)
    {
        var services = new TestApplicationBuilder(testOutputHelper).WithCapturingTextWriter(_capturingTextWriter).Build();
        _workflowRunner = services.GetRequiredService<IWorkflowRunner>();
    }

    [Theory(DisplayName = "The correct branch executes when condition is true")]
    [InlineData(true, "True!")]
    [InlineData(false, "False!")]
    public async Task Test1(bool conditionResult, string expectedLine)
    {
        var workflow = await new WorkflowDefinitionBuilder().BuildWorkflowAsync(new IfThenWorkflow(() => conditionResult));
        await _workflowRunner.RunAsync(workflow);
        var lines = _capturingTextWriter.Lines.ToList();
        Assert.Equal(new[] { expectedLine }, lines);
    }

    [Theory(DisplayName = "The If activity completes only after either one of its branches completed")]
    [InlineData(true, new[] { "Start", "Executing", "True!", "End" })]
    [InlineData(false, new[] { "Start", "Executing", "False!", "End" })]
    public async Task Test2(bool conditionResult, string[] expectedLines)
    {
        var workflow = await new WorkflowDefinitionBuilder().BuildWorkflowAsync(new ComplexIfWorkflow(() => conditionResult));
        await _workflowRunner.RunAsync(workflow);
        var lines = _capturingTextWriter.Lines.ToList();
        Assert.Equal(expectedLines, lines);
    }
}