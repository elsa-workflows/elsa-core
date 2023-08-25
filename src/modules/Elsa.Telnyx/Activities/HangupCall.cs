using System.Runtime.CompilerServices;
using Elsa.Extensions;
using Elsa.Workflows.Core;
using Elsa.Workflows.Core.Attributes;
using Elsa.Workflows.Core.Contracts;
using JetBrains.Annotations;

namespace Elsa.Telnyx.Activities;

/// <inheritdoc />
[PublicAPI]
public class HangupCall : HangupCallBase
{
    /// <inheritdoc />
    public HangupCall([CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) : base(source, line)
    {
    }
    
    /// <summary>
    /// The <see cref="IActivity"/> to execute when the call was no longer active.
    /// </summary>
    [Port] public IActivity? Disconnected { get; set; }

    /// <inheritdoc />
    protected override async ValueTask HandleDoneAsync(ActivityExecutionContext context) => await context.CompleteActivityAsync(OnCompletedAsync);

    /// <inheritdoc />
    protected override async ValueTask HandleDisconnectedAsync(ActivityExecutionContext context) => await context.ScheduleActivityAsync(Disconnected, OnCompletedAsync);
    
    /// <summary>
    /// Executed when any child activity completed.
    /// </summary>
    private async ValueTask OnCompletedAsync(ActivityCompletedContext context) => await context.TargetContext.CompleteActivityAsync();
}