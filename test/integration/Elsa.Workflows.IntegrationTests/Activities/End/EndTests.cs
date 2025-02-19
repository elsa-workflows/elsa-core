using Elsa.Testing.Shared;
using Elsa.Workflows.Contracts;
using Elsa.Workflows.IntegrationTests.Activities.Workflows;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstraction;

namespace Elsa.Workflows.IntegrationTests,Activities;
public class EndTests
{
    private readonly IWorkflowRunner _workflowRunner;
    private readonly CapturingTextWriter _capturingTextWriter = new();
    private readonly IServiceProvider _services;

    public EndTests(ITestOutputHelper testOutputHelper)
    {
        _services = new TestApplicationBuilder(testOutputHelper).WithCapturingTextWriter(_capturingTextWriter).Build();
        _workflowRunner = _services.GetRequiredService<IWorkflowRunner>();
    }

    [Fact(DisplayName = "End a flowchart when the End is nested in If")]
    public async Task Test1()
    {
        await _services.PopulateRegistriesAsync();
        await _workflowRunner.RunAsync<EndFlowchartWorkflow>();
        var lines = _capturingTextWriter.Lines.ToList();
        Assert.Equal(new[] { "start" }, lines);
    }
}
