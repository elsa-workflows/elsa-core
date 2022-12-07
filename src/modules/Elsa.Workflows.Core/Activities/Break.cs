using System.Runtime.CompilerServices;
using Elsa.Workflows.Core.Attributes;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Signals;

namespace Elsa.Workflows.Core.Activities;

/// <summary>
/// Break out of a loop.
/// </summary>
[Activity("Elsa", "Control Flow", "Break out of a loop.")]
public class Break : Activity
{
    /// <inheritdoc />
    public Break([CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) : base(source, line)
    {
    }
    
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        await context.SendSignalAsync(new BreakSignal());
    }
}