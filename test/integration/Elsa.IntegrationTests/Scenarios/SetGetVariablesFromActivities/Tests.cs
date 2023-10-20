using System.Linq;
using System.Threading.Tasks;
using Elsa.IntegrationTests.Scenarios.SetGetVariablesFromActivities.Workflows;
using Elsa.Testing.Shared;
using Elsa.Workflows.Core.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace Elsa.IntegrationTests.Scenarios.SetGetVariablesFromActivities;

public class Tests
{
    private readonly IWorkflowRunner _workflowRunner;
    private readonly CapturingTextWriter _capturingTextWriter = new();

    public Tests(ITestOutputHelper testOutputHelper)
    {
        var services = new TestApplicationBuilder(testOutputHelper).WithCapturingTextWriter(_capturingTextWriter).Build();
        services.GetRequiredService<IWorkflowBuilderFactory>();
        _workflowRunner = services.GetRequiredService<IWorkflowRunner>();
    }

    [Fact(DisplayName = "Activities can set and get variables internally")]
    public async Task Test1()
    {
        await _workflowRunner.RunAsync<SampleWorkflow>();
        var lines = _capturingTextWriter.Lines.ToList();
        Assert.Equal(new[] { "Bar" }, lines);
    }
}