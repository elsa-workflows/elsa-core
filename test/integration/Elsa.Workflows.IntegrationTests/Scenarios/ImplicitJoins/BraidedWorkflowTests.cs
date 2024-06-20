using Elsa.Testing.Shared;
using Elsa.Workflows.Contracts;
using Elsa.Workflows.IntegrationTests.Scenarios.ImplicitJoins.Workflows;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace Elsa.Workflows.IntegrationTests.Scenarios.ImplicitJoins;

public class BraidedWorkflowTests
{
    private readonly IWorkflowRunner _workflowRunner;
    private readonly CapturingTextWriter _capturingTextWriter = new();
    private readonly IServiceProvider _services;

    public BraidedWorkflowTests(ITestOutputHelper testOutputHelper)
    {
        _services = new TestApplicationBuilder(testOutputHelper).WithCapturingTextWriter(_capturingTextWriter).Build();
        _workflowRunner = _services.GetRequiredService<IWorkflowRunner>();
    }

    [Fact(DisplayName = "Braided workflows are executed correctly")]
    public async Task Test1()
    {
        await _services.PopulateRegistriesAsync();
        await _workflowRunner.RunAsync<BraidedWorkflow>();
        var lines = _capturingTextWriter.Lines.ToList();
        Assert.Equal(new[] { "WriteLine1", "WriteLine2", "WriteLine3", "WriteLine4", "WriteLine5", "WriteLine6", "WriteLine7" }, lines);
    }
    
    [Fact(DisplayName = "Braided workflows complete the workflow")]
    public async Task Test2()
    {
        await _services.PopulateRegistriesAsync();
        var result = await _workflowRunner.RunAsync<BraidedWorkflow>();
        Assert.Equal(WorkflowStatus.Finished, result.WorkflowState.Status);
    }
}