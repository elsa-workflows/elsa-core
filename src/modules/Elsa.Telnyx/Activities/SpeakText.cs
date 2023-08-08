using System.Runtime.CompilerServices;
using Elsa.Workflows.Core;
using Elsa.Workflows.Core.Attributes;
using Elsa.Workflows.Core.Contracts;
using JetBrains.Annotations;

namespace Elsa.Telnyx.Activities;

/// <inheritdoc />
[PublicAPI]
public class SpeakText : SpeakTextBase
{
    /// <inheritdoc />
    public SpeakText([CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) : base(source, line)
    {
    }

    /// <summary>
    /// The <see cref="IActivity"/> to execute when the call was no longer active.
    /// </summary>
    [Port]
    public IActivity? Disconnected { get; set; }

    /// <summary>
    /// The <see cref="IActivity"/> to execute when speaking has finished.
    /// </summary>
    [Port]
    public IActivity? FinishedSpeaking { get; set; }

    /// <inheritdoc />
    protected override async ValueTask HandleDisconnected(ActivityExecutionContext context) => await context.ScheduleActivityAsync(Disconnected);

    /// <inheritdoc />
    protected override async ValueTask HandleSpeakingHasFinished(ActivityExecutionContext context) => await context.ScheduleActivityAsync(FinishedSpeaking);
}