using System.Linq;
using System.Threading.Tasks;
using Elsa.Testing.Shared;
using Elsa.Workflows.Core.Contracts;
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
        await _workflowRunner.RunAsync(new IfThenWorkflow(() => conditionResult));
        var lines = _capturingTextWriter.Lines.ToList();
        Assert.Equal(new[] { expectedLine }, lines);
    }

    [Theory(DisplayName = "The If activity completes only after either one of its branches completed")]
    [InlineData(true, new[] { "Start", "Executing", "True!", "End" })]
    [InlineData(false, new[] { "Start", "Executing", "False!", "End" })]
    public async Task Test2(bool conditionResult, string[] expectedLines)
    {
        await _workflowRunner.RunAsync(new ComplexIfWorkflow(() => conditionResult));
        var lines = _capturingTextWriter.Lines.ToList();
        Assert.Equal(expectedLines, lines);
    }
}