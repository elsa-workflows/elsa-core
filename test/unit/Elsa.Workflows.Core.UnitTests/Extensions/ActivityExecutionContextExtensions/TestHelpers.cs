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
}
