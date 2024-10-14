using System.Runtime.CompilerServices;
using Elsa.Extensions;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Signals;
using JetBrains.Annotations;

namespace Elsa.Workflows.Activities;

/// <summary>
/// Break out of a loop.
/// </summary>
[Activity("Elsa", "Looping", "Break out of a loop.")]
[PublicAPI]
public class Break : CodeActivity, ITerminalNode
{
    /// <inheritdoc />
    public Break([CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) : base(source, line)
    {
    }

    /// <inheritdoc />
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        // Send a signal to the parent scope to break out of the loop.
        await context.SendSignalAsync(new BreakSignal());
    }
}