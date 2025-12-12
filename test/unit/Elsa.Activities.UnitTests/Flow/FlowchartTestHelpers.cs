using Elsa.Testing.Shared;
using Elsa.Workflows;
using Elsa.Workflows.Activities.Flowchart.Activities;
using Elsa.Workflows.Activities.Flowchart.Models;

namespace Elsa.Activities.UnitTests.Flow;

/// <summary>
/// Shared helper methods for Flowchart unit tests.
/// </summary>
public static class FlowchartTestHelpers
{
    public static async Task<ActivityExecutionContext> ExecuteFlowchartAsync(Flowchart flowchart, FlowchartExecutionMode? executionMode = null)
    {
        var fixture = new ActivityTestFixture(flowchart);

        if (executionMode.HasValue)
        {
            fixture.ConfigureContext(context =>
            {
                context.WorkflowExecutionContext.Properties[Flowchart.ExecutionModePropertyKey] = executionMode.Value;
            });
        }

        return await fixture.ExecuteAsync();
    }
}
