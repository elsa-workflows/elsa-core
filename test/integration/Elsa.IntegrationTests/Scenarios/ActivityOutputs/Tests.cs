using System.Linq;
using System.Threading.Tasks;
using Elsa.Testing.Shared;
using Elsa.Workflows.Core.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace Elsa.IntegrationTests.Scenarios.ActivityOutputs;

public class Tests
{
    private readonly IWorkflowRunner _workflowRunner;
    private readonly CapturingTextWriter _capturingTextWriter = new();

    public Tests(ITestOutputHelper testOutputHelper)
    {
        var services = new TestApplicationBuilder(testOutputHelper).WithCapturingTextWriter(_capturingTextWriter).Build();
        _workflowRunner = services.GetRequiredService<IWorkflowRunner>();
    }

    [Fact(DisplayName = "Activity outputs can be accessed from other activities.")]
    public async Task Test1()
    {
        await _workflowRunner.RunAsync<SumWorkflow>();
        var lines = _capturingTextWriter.Lines.ToList();
        Assert.Equal(new[]
        {
            "The result of 4 and 6 is 10.",
            "The last result is 10."
        }, lines);
    }
}