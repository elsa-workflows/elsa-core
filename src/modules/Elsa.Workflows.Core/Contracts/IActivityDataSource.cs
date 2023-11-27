namespace Elsa.Workflows.Core.Contracts;

/// <summary>
/// Represents a data source for an activity.
/// </summary>
public interface IActivityDataSource
{
    /// <summary>
    /// Gets the data for the specified workflow execution context.
    /// </summary>
    /// <param name="context">The activity execution context.</param>
    /// <returns>An enumerable of objects.</returns>
    IAsyncEnumerable<object> GetDataAsync(ActivityExecutionContext context);
}