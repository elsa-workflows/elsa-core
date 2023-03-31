namespace Elsa.Workflows.Core.Contracts;

/// <summary>
/// When implemented by an activity, it is invoked when the graph is built.
/// </summary>
public interface IInitializable : IActivity
{
    /// <summary>
    /// Called by the system to initialize the activity.
    /// </summary>
    ValueTask InitializeAsync(InitializationContext context);
}

/// <summary>
/// Provides access to contextual services and information.
/// </summary>
public record InitializationContext(IServiceProvider ServiceProvider, CancellationToken CancellationToken);