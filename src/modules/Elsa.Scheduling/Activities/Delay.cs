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
        Strategy = new(blockingStrategy);
    }

    /// <inheritdoc />
    public Delay(
        TimeSpan timeSpan, 
        DelayBlockingStrategy blockingStrategy = DelayBlockingStrategy.NonBlocking,
        [CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) : base(source, line)
    {
        TimeSpan = new Input<TimeSpan>(timeSpan);
        Strategy = new(blockingStrategy);
    }

    /// <inheritdoc />
    public Delay(
        Variable<TimeSpan> timeSpan, 
        DelayBlockingStrategy blockingStrategy = DelayBlockingStrategy.Blocking,
        [CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) : base(source, line)
    {
        TimeSpan = new Input<TimeSpan>(timeSpan);
        Strategy = new(blockingStrategy);
    }

    /// <summary>
    /// The amount of time to delay execution.
    /// </summary>
    [Input] public Input<TimeSpan> TimeSpan { get; set; } = default!;
    
    /// <summary>
    /// A value controlling whether the delay should happen in-process (synchronously or out of process (asynchronously).
    /// </summary>
    [Input(Description = "The strategy to use when delaying execution. Defaults to NonBlocking, which means that the workflow will be suspended and resumed at a later time. Blocking, on the other hand, will internally use Task.Delay and keep the workflow instance in memory.")] 
    public Input<DelayBlockingStrategy> Strategy { get; set; } = new(DelayBlockingStrategy.NonBlocking);
    
    /// <summary>
    /// The threshold used by the <see cref="DelayBlockingStrategy.Auto"/>
    /// </summary>
    [Input(Description = "The threshold used by the Auto strategy. If the time span is set to a value less than the threshold, the Blocking strategy will be used. Defaults to 5 seconds.")] 
    public Input<TimeSpan> AutoBlockingThreshold { get; set; } = new(System.TimeSpan.FromSeconds(5));

    /// <inheritdoc />
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var blockingMode = Strategy.Get(context);

        switch (blockingMode)
        {
            case DelayBlockingStrategy.Blocking:
                await BlockingStrategy(context);
                break;
                case DelayBlockingStrategy.NonBlocking:
                    await NonBlockingStrategy(context);
                    break;
            case DelayBlockingStrategy.Auto:
                await AutoBlockingStrategy(context);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private async ValueTask BlockingStrategy(ActivityExecutionContext context)
    {
        var timeSpan = context.ExpressionExecutionContext.Get(TimeSpan);
        await Task.Delay(timeSpan);
        await context.CompleteActivityAsync();
    }

    private ValueTask NonBlockingStrategy(ActivityExecutionContext context)
    {
        var clock = context.ExpressionExecutionContext.GetRequiredService<ISystemClock>();
        var timeSpan = context.ExpressionExecutionContext.Get(TimeSpan);
        var resumeAt = clock.UtcNow.Add(timeSpan);
        var payload = new DelayPayload(resumeAt);

        context.JournalData.Add("ResumeAt", resumeAt);
        context.CreateBookmark(payload);
        
        return ValueTask.CompletedTask;
    }

    private async ValueTask AutoBlockingStrategy(ActivityExecutionContext context)
    {
        var threshold = context.Get(AutoBlockingThreshold);
        var timeSpan = context.ExpressionExecutionContext.Get(TimeSpan);

        if (timeSpan <= threshold)
            await BlockingStrategy(context);
        else
            await NonBlockingStrategy(context);
    }

    /// <summary>
    /// Creates a new <see cref="Delay"/> from the specified number of milliseconds.
    /// </summary>
    public static Delay FromMilliseconds(
        double value, 
        DelayBlockingStrategy blockingStrategy = DelayBlockingStrategy.NonBlocking,
        [CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) => new(System.TimeSpan.FromMilliseconds(value), blockingStrategy, source, line);
    
    /// <summary>
    /// Creates a new <see cref="Delay"/> from the specified number of seconds.
    /// </summary>
    public static Delay FromSeconds(
        double value, 
        DelayBlockingStrategy blockingStrategy = DelayBlockingStrategy.NonBlocking,
        [CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) => new(System.TimeSpan.FromSeconds(value), blockingStrategy, source, line);
    
    /// <summary>
    /// Creates a new <see cref="Delay"/> from the specified number of minutes.
    /// </summary>
    public static Delay FromMinutes(
        double value, 
        DelayBlockingStrategy blockingStrategy = DelayBlockingStrategy.NonBlocking,
        [CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) => new(System.TimeSpan.FromMinutes(value), blockingStrategy, source, line);
    
    /// <summary>
    /// Creates a new <see cref="Delay"/> from the specified number of hours.
    /// </summary>
    public static Delay FromHours(
        double value, 
        DelayBlockingStrategy blockingStrategy = DelayBlockingStrategy.NonBlocking,
        [CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) => new(System.TimeSpan.FromHours(value), blockingStrategy, source, line);
    
    /// <summary>
    /// Creates a new <see cref="Delay"/> from the specified number of days.
    /// </summary>
    public static Delay FromDays(
        double value, 
        DelayBlockingStrategy blockingStrategy = DelayBlockingStrategy.NonBlocking,
        [CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) => new(System.TimeSpan.FromDays(value), blockingStrategy, source, line);
}

/// <summary>
/// A bookmark payload for <see cref="Delay"/>.
/// </summary>
public record DelayPayload(DateTimeOffset ResumeAt);