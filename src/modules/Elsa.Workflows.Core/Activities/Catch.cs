using System.Runtime.CompilerServices;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using Elsa.Workflows.UIHints;

namespace Elsa.Workflows.Activities;

/// <summary>
/// Catch the Exception.
/// </summary>
[Activity("Elsa", "Primitives", "Catch the Exception.")]
public class Catch : Activity
{    
    /// <inheritdoc />
    public Catch([CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) : base(source, line)
    {
    }

    /// <summary>
    /// A value indicating whether to catch all exceptions from all activities.
    /// </summary>
    [Input(Description = "A value indicating whether to catch all exceptions from all activities.")]
    public Input<bool> CatchAll { get; set; } = default!;

    /// <summary>
    /// The IDs of the activities to catch exceptions from.
    /// </summary>
    [Input(Description = "The IDs of the activities to catch exceptions from.",
        UIHint = InputUIHints.MultiText)]
    public Input<ICollection<string>?> CatchActivities { get; set; } = default!;

    /// <inheritdoc />
    protected override ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        return context.CompleteActivityAsync();
    }
}
