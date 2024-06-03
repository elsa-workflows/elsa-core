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
    /// Catch all the Exception.
    /// </summary>
    [Input(Description = "A value that is the tag a global catcher")]
    public Input<bool> CatchAll { get; set; } = default!;

    /// <summary>
    /// Catch the Activities.
    /// </summary>
    [Input(Description = "The Activities will be catch",
        UIHint = InputUIHints.MultiText)]
    public Input<ICollection<string>?> CatchActivities { get; set; } = default!;

    /// <inheritdoc />
    protected override ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        return context.CompleteActivityAsync();
    }
}
