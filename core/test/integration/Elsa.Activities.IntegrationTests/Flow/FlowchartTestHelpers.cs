using Elsa.Testing.Shared;
using Elsa.Workflows;
using Elsa.Workflows.Activities.Flowchart.Activities;
using Elsa.Workflows.Activities.Flowchart.Extensions;
using Elsa.Workflows.Activities.Flowchart.Models;
using Elsa.Workflows.Models;
using Elsa.Workflows.Options;
using Xunit.Abstractions;

namespace Elsa.Activities.IntegrationTests.Flow;

/// <summary>
/// Shared helper methods for Flowchart integration tests.
/// </summary>
public static class FlowchartTestHelpers
{
    public static IServiceProvider CreateServiceProvider(ITestOutputHelper testOutputHelper, CapturingTextWriter? capturingTextWriter = null)
    {
        var builder = new TestApplicationBuilder(testOutputHelper);
        if (capturingTextWriter != null)
            builder.WithCapturingTextWriter(capturingTextWriter);
        return builder.Build();
    }

    public static async Task<RunWorkflowResult> RunFlowchartAsync(IServiceProvider services, Flowchart flowchart, FlowchartExecutionMode? executionMode = null)
    {
        var options = executionMode.HasValue
            ? new RunWorkflowOptions().WithFlowchartExecutionMode(executionMode.Value)
            : null;

        return await services.RunActivityAsync(flowchart, options);
    }

    public static Connection CreateConnection(IActivity source, IActivity target, string? outcome = "Done")
    {
        return new(new(source, outcome), new Endpoint(target));
    }

    public static Flowchart CreateSimpleLinearFlowchart(params IActivity[] activities)
    {
        var flowchart = new Flowchart
        {
            Start = activities.FirstOrDefault(),
            Activities = new List<IActivity>(activities)
        };

        for (var i = 0; i < activities.Length - 1; i++)
        {
            flowchart.Connections.Add(CreateConnection(activities[i], activities[i + 1]));
        }

        return flowchart;
    }

    public static Flowchart CreateBranchingFlowchart(IActivity start, IActivity branch1, IActivity branch2, IActivity? join = null)
    {
        var flowchart = new Flowchart
        {
            Start = start,
            Activities = { start, branch1, branch2 }
        };

        flowchart.Connections.Add(CreateConnection(start, branch1));
        flowchart.Connections.Add(CreateConnection(start, branch2));

        if (join == null)
            return flowchart;

        flowchart.Activities.Add(join);
        flowchart.Connections.Add(CreateConnection(branch1, join));
        flowchart.Connections.Add(CreateConnection(branch2, join));

        return flowchart;
    }
}
