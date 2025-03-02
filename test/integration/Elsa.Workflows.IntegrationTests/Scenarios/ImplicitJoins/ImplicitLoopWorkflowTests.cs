using Elsa.Testing.Shared;
using Elsa.Workflows.IntegrationTests.Scenarios.ImplicitJoins.Workflows;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace Elsa.Workflows.IntegrationTests.Scenarios.ImplicitJoins;

public class ImplicitWorkflowTests
{
    private readonly IWorkflowRunner _workflowRunner;
    private readonly CapturingTextWriter _capturingTextWriter = new();
    private readonly IServiceProvider _services;

    public ImplicitWorkflowTests(ITestOutputHelper testOutputHelper)
    {
        _services = new TestApplicationBuilder(testOutputHelper).WithCapturingTextWriter(_capturingTextWriter).Build();
        _workflowRunner = _services.GetRequiredService<IWorkflowRunner>();
    }

    [Fact(DisplayName = "Implicit loop workflows are executed correctly")]
    public async Task Test1()
    {
        await _services.PopulateRegistriesAsync();
        await _workflowRunner.RunAsync<ImplicitLoopWorkflow>();
        var lines = _capturingTextWriter.Lines.ToList();
        Assert.Equal(new[] { "Start", "Retry", "End" }, lines);
    }
    
    [Fact(DisplayName = "Implicit loop workflows complete the workflow")]
    public async Task Test2()
    {
        await _services.PopulateRegistriesAsync();
        var result = await _workflowRunner.RunAsync<ImplicitLoopWorkflow>();
        Assert.Equal(WorkflowStatus.Finished, result.WorkflowState.Status);
    }
}