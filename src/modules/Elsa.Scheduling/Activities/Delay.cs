using Elsa.Common.Services;
using Elsa.Scheduling.Models;
using Elsa.Workflows.Core;
using Elsa.Workflows.Core.Attributes;
using Elsa.Workflows.Core.Models;

namespace Elsa.Scheduling.Activities;

[Activity( "Elsa", "Scheduling", "Delay execution for the specified amount of time.")]
public class Delay : Activity
{
    public Delay()
    {
    }

    public Delay(Input<TimeSpan> timeSpan) => TimeSpan = timeSpan;
    public Delay(TimeSpan timeSpan) => TimeSpan = new Input<TimeSpan>(timeSpan);
    public Delay(Variable<TimeSpan> timeSpan) => TimeSpan = new Input<TimeSpan>(timeSpan);

    [Input] public Input<TimeSpan> TimeSpan { get; set; } = default!;
    
    [Input] public Input<DelayBlockingStrategy> Strategy { get; set; } = default!;
    
    /// <summary>
    /// The threshold used by the <see cref="DelayBlockingStrategy.Auto"/>
    /// </summary>
    [Input] public Input<TimeSpan> AutoBlockingThreshold { get; set; } = new(System.TimeSpan.FromSeconds(5));

    /// <inheritdoc />
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var timeSpan = context.ExpressionExecutionContext.Get(TimeSpan);
        var blockingMode = context.Get(Strategy);

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

    public static Delay FromMilliseconds(double value) => new(System.TimeSpan.FromMilliseconds(value));
    public static Delay FromSeconds(double value) => new(System.TimeSpan.FromSeconds(value));
    public static Delay FromMinutes(double value) => new(System.TimeSpan.FromMinutes(value));
    public static Delay FromHours(double value) => new(System.TimeSpan.FromHours(value));
    public static Delay FromDays(double value) => new(System.TimeSpan.FromDays(value));
}

public record DelayPayload(DateTimeOffset ResumeAt);