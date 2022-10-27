using System.Collections.Generic;
using System.Threading.Tasks;
using Elsa.Workflows.Core.Services;

namespace Elsa.Workflows.Core.Models;

/// <summary>
/// A dynamically provided activity with custom properties.
/// </summary>
public class DynamicActivity : Activity
{
    public IDictionary<string, object?> Properties { get; set; } = new Dictionary<string, object?>();
    public ExecuteActivityDelegate ExecuteHandler { get; set; } = _ => ValueTask.CompletedTask;

    protected override ValueTask ExecuteAsync(ActivityExecutionContext context) => ExecuteHandler(context);
}