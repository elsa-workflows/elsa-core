using System.Runtime.CompilerServices;
using Elsa.Workflows.Core;
using Elsa.Workflows.Core.Attributes;
using Elsa.Workflows.Core.Contracts;
using JetBrains.Annotations;

namespace Elsa.Telnyx.Activities;

/// <inheritdoc />
[PublicAPI]
public class BridgeCalls : BridgeCallsBase
{
    /// <inheritdoc />
    public BridgeCalls([CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) : base(source, line)
    {
    }

    /// <summary>
    /// The <see cref="IActivity"/> to execute when the source leg call is no longer active.
    /// </summary>
    [Port]
    public IActivity? Disconnected { get; set; }

    /// <summary>
    /// The <see cref="IActivity"/> to execute when the two calls are bridged.
    /// </summary>
    [Port]
    public IActivity? Bridged { get; set; }

    /// <inheritdoc />
    protected override async ValueTask HandleDisconnectedAsync(ActivityExecutionContext context) => await context.ScheduleActivityAsync(Disconnected, OnCompleted);

    /// <inheritdoc />
    protected override async ValueTask HandleBridgedAsync(ActivityExecutionContext context) => await context.ScheduleActivityAsync(Bridged, OnCompleted);
}