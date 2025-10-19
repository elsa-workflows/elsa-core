using Elsa.Testing.Shared;
using Elsa.Workflows;
using Elsa.Workflows.Activities.Flowchart.Activities;

namespace Elsa.Activities.UnitTests.Flow;

/// <summary>
/// Shared helper methods for Flowchart unit tests.
/// </summary>
public static class FlowchartTestHelpers
{
    public static async Task<ActivityExecutionContext> ExecuteFlowchartAsync(Flowchart flowchart)
    {
        var fixture = new ActivityTestFixture(flowchart);
        return await fixture.ExecuteAsync();
    }
}
