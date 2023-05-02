using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using Elsa.Common.Contracts;
using Elsa.Expressions.Models;
using Elsa.Extensions;
using Elsa.Scheduling.Models;
using Elsa.Workflows.Core.Attributes;
using Elsa.Workflows.Core.Models;

namespace Elsa.Scheduling.Activities;

/// <summary>
/// Delay execution for the specified amount of time.
/// </summary>
[Activity( "Elsa", "Scheduling", "Delay execution for the specified amount of time.")]
public class Delay : Activity
{
    /// <inheritdoc />
    [JsonConstructor]
    public Delay([CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) : base(source, line)
    {
    }

    /// <inheritdoc />
    public Delay(
        Func<ExpressionExecutionContext, TimeSpan> timeSpan,
        [CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) : this(new Input<TimeSpan>(timeSpan), source, line)
    {
    }

    /// <inheritdoc />
    public Delay(
        Func<ExpressionExecutionContext, ValueTask<TimeSpan>> timeSpan,
        [CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) : this(new Input<TimeSpan>(timeSpan), source, line)
    {
    }

    /// <inheritdoc />
    public Delay(
        Input<TimeSpan> timeSpan,
        [CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) : base(source, line)
    {
        TimeSpan = timeSpan;
    }

    /// <inheritdoc />
    public Delay(
        TimeSpan timeSpan,
        [CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) : base(source, line)
    {
        TimeSpan = new Input<TimeSpan>(timeSpan);
    }

    /// <inheritdoc />
    public Delay(
        Variable<TimeSpan> timeSpan,
        [CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) : base(source, line)
    {
        TimeSpan = new Input<TimeSpan>(timeSpan);
    }

    /// <summary>
    /// The amount of time to delay execution.
    /// </summary>
    [Input] public Input<TimeSpan> TimeSpan { get; set; } = default!;

    /// <inheritdoc />
    protected override void Execute(ActivityExecutionContext context)
    {
        var clock = context.ExpressionExecutionContext.GetRequiredService<ISystemClock>();
        var timeSpan = context.ExpressionExecutionContext.Get(TimeSpan);
        var resumeAt = clock.UtcNow.Add(timeSpan);
        var payload = new DelayPayload(resumeAt);

        context.JournalData.Add("ResumeAt", resumeAt);
        context.CreateBookmark(payload);
    }
    
    /// <summary>
    /// Creates a new <see cref="Delay"/> from the specified number of milliseconds.
    /// </summary>
    public static Delay FromMilliseconds(
        double value,
        [CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) => new(System.TimeSpan.FromMilliseconds(value), source, line);
    
    /// <summary>
    /// Creates a new <see cref="Delay"/> from the specified number of seconds.
    /// </summary>
    public static Delay FromSeconds(
        double value,
        [CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) => new(System.TimeSpan.FromSeconds(value), source, line);
    
    /// <summary>
    /// Creates a new <see cref="Delay"/> from the specified number of minutes.
    /// </summary>
    public static Delay FromMinutes(
        double value,
        [CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) => new(System.TimeSpan.FromMinutes(value), source, line);
    
    /// <summary>
    /// Creates a new <see cref="Delay"/> from the specified number of hours.
    /// </summary>
    public static Delay FromHours(
        double value,
        [CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) => new(System.TimeSpan.FromHours(value), source, line);
    
    /// <summary>
    /// Creates a new <see cref="Delay"/> from the specified number of days.
    /// </summary>
    public static Delay FromDays(
        double value,
        [CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) => new(System.TimeSpan.FromDays(value), source, line);
}

/// <summary>
/// A bookmark payload for <see cref="Delay"/>.
/// </summary>
public record DelayPayload(DateTimeOffset ResumeAt);