using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using Elsa.Common.Services;
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
public class Delay : CodeActivity
{
    /// <inheritdoc />
    [JsonConstructor]
    public Delay([CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) : base(source, line)
    {
    }

    /// <inheritdoc />
    public Delay(
        Func<ExpressionExecutionContext, TimeSpan> timeSpan, 
        DelayBlockingStrategy blockingStrategy = DelayBlockingStrategy.NonBlocking,
        [CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) : this(new Input<TimeSpan>(timeSpan), blockingStrategy, source, line)
    {
    }

    /// <inheritdoc />
    public Delay(
        Func<ExpressionExecutionContext, ValueTask<TimeSpan>> timeSpan, 
        DelayBlockingStrategy blockingStrategy = DelayBlockingStrategy.NonBlocking,
        [CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) : this(new Input<TimeSpan>(timeSpan), blockingStrategy, source, line)
    {
    }

    /// <inheritdoc />
    public Delay(
        Input<TimeSpan> timeSpan, 
        DelayBlockingStrategy blockingStrategy = DelayBlockingStrategy.NonBlocking,
        [CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) : base(source, line)
    {
        TimeSpan = timeSpan;
        Strategy = blockingStrategy;
    }

    /// <inheritdoc />
    public Delay(
        TimeSpan timeSpan, 
        DelayBlockingStrategy blockingStrategy = DelayBlockingStrategy.NonBlocking,
        [CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) : base(source, line)
    {
        TimeSpan = new Input<TimeSpan>(timeSpan);
        Strategy = blockingStrategy;
    }

    /// <inheritdoc />
    public Delay(
        Variable<TimeSpan> timeSpan, 
        DelayBlockingStrategy blockingStrategy = DelayBlockingStrategy.NonBlocking,
        [CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) : base(source, line)
    {
        TimeSpan = new Input<TimeSpan>(timeSpan);
        Strategy = blockingStrategy;
    }

    /// <summary>
    /// The amount of time to delay execution.
    /// </summary>
    [Input] public Input<TimeSpan> TimeSpan { get; set; } = default!;
    
    /// <summary>
    /// A value controlling whether the delay should happen in-process (synchronously or out of process (asynchronously).
    /// </summary>
    [Input] public DelayBlockingStrategy Strategy { get; set; } = DelayBlockingStrategy.NonBlocking;
    
    /// <summary>
    /// The threshold used by the <see cref="DelayBlockingStrategy.Auto"/>
    /// </summary>
    [Input] public Input<TimeSpan> AutoBlockingThreshold { get; set; } = new(System.TimeSpan.FromSeconds(5));

    /// <inheritdoc />
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var timeSpan = context.ExpressionExecutionContext.Get(TimeSpan);
        var blockingMode = Strategy;

        switch (blockingMode)
        {
            case DelayBlockingStrategy.Blocking:
                await BlockingStrategy(timeSpan);
                break;
                case DelayBlockingStrategy.NonBlocking:
                    await NonBlockingStrategy(timeSpan, context);
                    break;
            case DelayBlockingStrategy.Auto:
                await AutoBlockingStrategy(timeSpan, context);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private async ValueTask BlockingStrategy(TimeSpan timeSpan) => await Task.Delay(timeSpan);

    private ValueTask NonBlockingStrategy(TimeSpan timeSpan, ActivityExecutionContext context)
    {
        var clock = context.ExpressionExecutionContext.GetRequiredService<ISystemClock>();
        var resumeAt = clock.UtcNow.Add(timeSpan);
        var payload = new DelayPayload(resumeAt);

        context.JournalData.Add("ResumeAt", resumeAt);
        context.CreateBookmark(payload);
        
        return ValueTask.CompletedTask;
    }

    private async ValueTask AutoBlockingStrategy(TimeSpan timeSpan, ActivityExecutionContext context)
    {
        var threshold = context.Get(AutoBlockingThreshold);

        if (timeSpan <= threshold)
            await BlockingStrategy(timeSpan);
        else
            await NonBlockingStrategy(timeSpan, context);
    }

    /// <summary>
    /// Creates a new <see cref="Delay"/> from the specified number of milliseconds.
    /// </summary>
    public static Delay FromMilliseconds(double value, DelayBlockingStrategy blockingStrategy = DelayBlockingStrategy.NonBlocking) => new(System.TimeSpan.FromMilliseconds(value), blockingStrategy);
    
    /// <summary>
    /// Creates a new <see cref="Delay"/> from the specified number of seconds.
    /// </summary>
    public static Delay FromSeconds(double value, DelayBlockingStrategy blockingStrategy = DelayBlockingStrategy.NonBlocking) => new(System.TimeSpan.FromSeconds(value), blockingStrategy);
    
    /// <summary>
    /// Creates a new <see cref="Delay"/> from the specified number of minutes.
    /// </summary>
    public static Delay FromMinutes(double value, DelayBlockingStrategy blockingStrategy = DelayBlockingStrategy.NonBlocking) => new(System.TimeSpan.FromMinutes(value), blockingStrategy);
    
    /// <summary>
    /// Creates a new <see cref="Delay"/> from the specified number of hours.
    /// </summary>
    public static Delay FromHours(double value, DelayBlockingStrategy blockingStrategy = DelayBlockingStrategy.NonBlocking) => new(System.TimeSpan.FromHours(value), blockingStrategy);
    
    /// <summary>
    /// Creates a new <see cref="Delay"/> from the specified number of days.
    /// </summary>
    public static Delay FromDays(double value, DelayBlockingStrategy blockingStrategy = DelayBlockingStrategy.NonBlocking) => new(System.TimeSpan.FromDays(value), blockingStrategy);
}

/// <summary>
/// A bookmark payload for <see cref="Delay"/>.
/// </summary>
public record DelayPayload(DateTimeOffset ResumeAt);