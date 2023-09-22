using System;
using System.Linq;
using System.Threading.Tasks;
using Elsa.IntegrationTests.Scenarios.FlowchartNextActivity.Workflows;
using Elsa.Testing.Shared;
using Elsa.Workflows.Core.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace Elsa.IntegrationTests.Scenarios.FlowchartNextActivity;

public class FlowchartNextActivityTests
{
    private readonly CapturingTextWriter _capturingTextWriter = new();
    private readonly IServiceProvider _services;
    private readonly IWorkflowRunner _workflowRunner;

    public FlowchartNextActivityTests(ITestOutputHelper testOutputHelper)
    {
        _services = new TestApplicationBuilder(testOutputHelper)
            .WithCapturingTextWriter(_capturingTextWriter)
            .AddActivitiesFrom<FlowchartNextActivityTests>()
            .Build();

        _workflowRunner = _services.GetRequiredService<IWorkflowRunner>();
    }

    [Fact(DisplayName = "Flowchart only schedules next activity connected to outcome of previous activity.")]
    public async Task Test1()
    {
        await _services.PopulateRegistriesAsync();
        await _workflowRunner.RunAsync<FlowchartWorkflow>();
        var lines = _capturingTextWriter.Lines.ToList();
        Assert.Equal(new[] { "Line 1" }, lines);
    }
}