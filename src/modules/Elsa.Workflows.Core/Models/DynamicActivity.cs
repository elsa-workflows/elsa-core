using System.ComponentModel;
using Elsa.Workflows.Core.Services;

namespace Elsa.Workflows.Core.Models;

/// <summary>
/// A dynamically provided activity with custom properties. This is experimental and may be removed.
/// </summary>
[Browsable(false)]
public class DynamicActivity : CodeActivity
{
    public IDictionary<string, object?> Properties { get; set; } = new Dictionary<string, object?>();
    public ExecuteActivityDelegate ExecuteHandler { get; set; } = _ => ValueTask.CompletedTask;

    protected override ValueTask ExecuteAsync(ActivityExecutionContext context) => ExecuteHandler(context);
}