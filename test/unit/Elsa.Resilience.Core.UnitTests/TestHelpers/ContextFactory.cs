using Elsa.Testing.Shared;
using Elsa.Workflows;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Resilience.Core.UnitTests.TestHelpers;

internal static class ContextFactory
{
    /// <summary>
    /// Builds a real <see cref="ActivityExecutionContext"/> for <paramref name="activity"/> without executing it.
    /// </summary>
    public static Task<ActivityExecutionContext> CreateAsync(IActivity activity, Action<IServiceCollection>? configureServices = null)
    {
        var fixture = new ActivityTestFixture(activity);

        if (configureServices != null)
            fixture.ConfigureServices(configureServices);

        return fixture.BuildAsync();
    }
}
