using Elsa.Testing.Shared;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace Elsa.Workflows.IntegrationTests.Scenarios.ActivityOutputs;

public class Tests
{
    private readonly IWorkflowRunner _workflowRunner;
    private readonly CapturingTextWriter _capturingTextWriter = new();
    private readonly IServiceProvider _services;

    public Tests(ITestOutputHelper testOutputHelper)
    {
        _services = new TestApplicationBuilder(testOutputHelper).WithCapturingTextWriter(_capturingTextWriter).Build();
        _workflowRunner = _services.GetRequiredService<IWorkflowRunner>();
    }

    [Fact(DisplayName = "Activity outputs can be accessed from other activities.")]
    public async Task Test1()
    {
        await _services.PopulateRegistriesAsync();
        await _workflowRunner.RunAsync<SumWorkflow>();
        var lines = _capturingTextWriter.Lines.ToList();
        Assert.Equal(new[]
        {
            "The result of 4 and 6 is 10.", "The last result is 10."
        }, lines);
    }

    [Fact(DisplayName = "The last activity output is returned.")]
    public async Task Test2()
    {
        await _services.PopulateRegistriesAsync();
        await _workflowRunner.RunAsync<LoopingWorkflow>();
        var lines = _capturingTextWriter.Lines.ToList();
        Assert.Equal(new[]
        {
            "Current value: Item 1, Activity result: Item 1",
            "Current value: Item 2, Activity result: Item 2",
            "Current value: Item 3, Activity result: Item 3"
        }, lines);
    }
}