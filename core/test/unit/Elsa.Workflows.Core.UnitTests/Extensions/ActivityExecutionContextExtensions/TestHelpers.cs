using Elsa.Testing.Shared;
using Elsa.Workflows.Activities;

namespace Elsa.Workflows.Core.UnitTests.Extensions.ActivityExecutionContextExtensions;

/// <summary>
/// Provides helper methods for ActivityExecutionContextExtensions unit tests.
/// </summary>
public static class TestHelpers
{
    /// <summary>
    /// Creates an ActivityExecutionContext for testing purposes.
    /// </summary>
    /// <param name="activity">Optional activity to use. If null, a default WriteLine activity is created.</param>
    /// <returns>A configured ActivityExecutionContext ready for testing.</returns>
    public static Task<ActivityExecutionContext> CreateContextAsync(IActivity? activity = null)
    {
        activity ??= new WriteLine("test");
        var fixture = new ActivityTestFixture(activity);
        return fixture.BuildAsync();
    }

    /// <summary>
    /// Creates a chain of nested activity execution contexts for testing ancestor-sensitive behavior.
    /// </summary>
    /// <param name="depth">The number of contexts to create. Must be at least two.</param>
    /// <returns>The contexts, ordered from the outermost ancestor to the innermost descendant.</returns>
    public static async Task<IReadOnlyList<ActivityExecutionContext>> CreateContextChainAsync(int depth = 3)
    {
        // Every level is a Sequence so that a single registered activity type covers the whole chain.
        var activities = Enumerable.Range(0, depth).Select(_ => new Sequence()).ToList();

        for (var i = 0; i < depth - 1; i++)
            activities[i].Activities.Add(activities[i + 1]);

        var contexts = new List<ActivityExecutionContext>
        {
            await CreateContextAsync(activities[0])
        };

        for (var i = 1; i < depth; i++)
            contexts.Add(await contexts[0].WorkflowExecutionContext.CreateActivityExecutionContextAsync(activities[i], new() { Owner = contexts[i - 1] }));

        return contexts;
    }
}
