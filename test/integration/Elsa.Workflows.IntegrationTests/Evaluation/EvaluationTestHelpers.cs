using Elsa.Testing.Shared;
using Elsa.Workflows;
using Elsa.Workflows.Activities;

namespace Elsa.Workflows.IntegrationTests.Evaluation;

/// <summary>
/// Shared test helpers for input evaluation tests.
/// </summary>
public static class EvaluationTestHelpers
{
    /// <summary>
    /// Creates an ActivityExecutionContext for any activity.
    /// </summary>
    public static async Task<ActivityExecutionContext> CreateContextAsync<TActivity>(TActivity activity)
        where TActivity : IActivity
    {
        var fixture = new ActivityTestFixture(activity);
        return await fixture.BuildAsync();
    }
}
