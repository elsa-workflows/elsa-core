using Elsa.Contracts;

namespace Elsa.Models;

/// <summary>
/// A dynamically provided activity with custom properties.
/// </summary>
public class DynamicActivity : Activity
{
    public IDictionary<string, object?> Properties { get; set; } = new Dictionary<string, object?>();
    public ExecuteActivityDelegate ExecuteHandler { get; set; } = _ => ValueTask.CompletedTask;

    public override ValueTask ExecuteAsync(ActivityExecutionContext context) => ExecuteHandler(context);
}