using Elsa.Testing.Shared;
using Elsa.Workflows.IntegrationTests.Activities.Workflows;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace Elsa.Workflows.IntegrationTests.Activities;

public class BreakTests
{
    private readonly IWorkflowRunner _workflowRunner;
    private readonly CapturingTextWriter _capturingTextWriter = new();
    private readonly IServiceProvider _services;

    public BreakTests(ITestOutputHelper testOutputHelper)
    {
        _services = new TestApplicationBuilder(testOutputHelper).WithCapturingTextWriter(_capturingTextWriter).Build();
        _workflowRunner = _services.GetRequiredService<IWorkflowRunner>();
    }

    [Fact(DisplayName = "Break exits out of ForEach")]
    public async Task Test1()
    {
        await _services.PopulateRegistriesAsync();
        await _workflowRunner.RunAsync<BreakForEachWorkflow>();
        var lines = _capturingTextWriter.Lines.ToList();
        Assert.Equal(new[] { "Start", "C#", "End" }, lines);
    }

    [Fact(DisplayName = "Break exits out of immediate ForEach only")]
    public async Task Test2()
    {
        await _services.PopulateRegistriesAsync();
        await _workflowRunner.RunAsync<NestedForEachWithBreakWorkflow>();
        var lines = _capturingTextWriter.Lines.ToList();
        Assert.Equal(new[] { "C#", "Classes", "Rust", "Classes", "Go", "Classes" }, lines);
    }

    [Fact(DisplayName = "Break exits out of For")]
    public async Task Test3()
    {
        await _services.PopulateRegistriesAsync();
        await _workflowRunner.RunAsync<BreakForWorkflow>();
        var lines = _capturingTextWriter.Lines.ToList();
        Assert.Equal(new[] { "Start", "0", "1", "End" }, lines);
    }
    
    [Fact(DisplayName = "Break exits out of While")]
    public async Task Test4()
    {
        await _services.PopulateRegistriesAsync();
        await _workflowRunner.RunAsync<BreakWhileWorkflow>();
        var lines = _capturingTextWriter.Lines.ToList();
        Assert.Equal(new[] { "Start", "1", "2", "End" }, lines);
    }
    
    [Fact(DisplayName = "Break removes the right bookmarks")]
    public async Task Test5()
    {
        await _services.PopulateRegistriesAsync();
        await _workflowRunner.RunAsync<BreakWhileWorkflow>();
        var lines = _capturingTextWriter.Lines.ToList();
        Assert.Equal(new[] { "Start", "1", "2", "End" }, lines);
    }

    [Fact(DisplayName = "Break While whin a Flowchart")]
    public async Task Test6()
    {
        await _services.PopulateRegistriesAsync();
        await _workflowRunner.RunAsync<BreakWhileFlowchartWorkflow>();
        var lines = _capturingTextWriter.Lines.ToList();
        Assert.Equal(new[] { "start" }, lines);
    }
}
