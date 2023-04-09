namespace Elsa.Scheduling.Models;

/// <summary>
/// Represents a task execution context.
/// </summary>
/// <param name="ServiceProvider">The service provider.</param>
/// <param name="CancellationToken">The cancellation token.</param>
public record TaskExecutionContext(IServiceProvider ServiceProvider, CancellationToken CancellationToken);