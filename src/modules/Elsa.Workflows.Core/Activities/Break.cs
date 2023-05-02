using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using Elsa.Extensions;
using Elsa.Workflows.Core.Attributes;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Signals;

namespace Elsa.Workflows.Core.Activities;

/// <summary>
/// Break out of a loop.
/// </summary>
[Activity("Elsa", "Looping", "Break out of a loop.")]
public class Break : CodeActivity
{
    /// <inheritdoc />
    [JsonConstructor]
    public Break()
    {
    }
    
    /// <inheritdoc />
    public Break([CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) : base(source, line)
    {
    }
    
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        await context.SendSignalAsync(new BreakSignal());
    }
}