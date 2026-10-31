using System.Runtime.CompilerServices;
using Elsa.Expressions.Models;
using Elsa.Extensions;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;

namespace Elsa.Scheduling.Activities;

/// <summary>
/// Represents a timer to periodically trigger the workflow.
/// </summary>
[Activity("Elsa", "Scheduling", "Trigger workflow execution at a specific interval.")]
public class Timer : TimerBase
{
    /// <inheritdoc />
    public Timer([CallerFilePath] string? source = null, [CallerLineNumber] int? line = null) : base(source, line)
    {
    }

    /// <inheritdoc />
    public Timer(TimeSpan interval, [CallerFilePath] string? source = null, [CallerLineNumber] int? line = null) : this(new Input<TimeSpan>(interval), source, line)
    {
    }

    /// <inheritdoc />
    public Timer(Input<TimeSpan> interval, [CallerFilePath] string? source = null, [CallerLineNumber] int? line = null) : this(source, line)
    {
        Interval = interval;
    }

    /// <summary>
    /// The interval at which the timer should execute.
    /// </summary>
    [Input(Description = "The interval at which the timer should execute.", DefaultValue = "00:01:00")]
    public Input<TimeSpan> Interval { get; set; } = new(TimeSpan.FromMinutes(1));

    protected override TimeSpan GetInterval(ExpressionExecutionContext context)
    {
        return Interval.Get(context);
    }

    /// <summary>
    /// Creates a new <see cref="Timer"/> activity set to trigger at the specified interval.
    /// </summary>
    public static Timer FromTimeSpan(TimeSpan value, [CallerFilePath] string? source = null, [CallerLineNumber] int? line = null) => new(value, source, line);

    /// <summary>
    /// Creates a new <see cref="Timer"/> activity set to trigger at the specified interval in seconds.
    /// </summary>
    public static Timer FromSeconds(double value, [CallerFilePath] string? source = null, [CallerLineNumber] int? line = null) => FromTimeSpan(TimeSpan.FromSeconds(value), source, line);
}