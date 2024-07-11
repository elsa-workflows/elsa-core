using Elsa.Testing.Shared;
using Elsa.Workflows.Contracts;
using Elsa.Workflows.IntegrationTests.Scenarios.SetGetVariablesFromActivities.Workflows;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace Elsa.Workflows.IntegrationTests.Scenarios.SetGetVariablesFromActivities;

public class Tests
{
    private readonly IWorkflowRunner _workflowRunner;
    private readonly CapturingTextWriter _capturingTextWriter = new();
    private readonly IServiceProvider _services;

    public Tests(ITestOutputHelper testOutputHelper)
    {
        _services = new TestApplicationBuilder(testOutputHelper).WithCapturingTextWriter(_capturingTextWriter).Build();
        _services.GetRequiredService<IWorkflowBuilderFactory>();
        _workflowRunner = _services.GetRequiredService<IWorkflowRunner>();
    }

    [Fact(DisplayName = "Activities can set and get variables internally")]
    public async Task Test1()
    {
        await _services.PopulateRegistriesAsync(); 
        await _workflowRunner.RunAsync<SampleWorkflow>();
        var lines = _capturingTextWriter.Lines.ToList();
        Assert.Equal(new[] { "Bar" }, lines);
    }
}