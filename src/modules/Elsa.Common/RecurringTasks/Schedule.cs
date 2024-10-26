namespace Elsa.Common.RecurringTasks;

public class Schedule
{
    public IntervalExpressionType Type { get; set; } = IntervalExpressionType.Interval;
    public string Expression { get; set; } = TimeSpan.FromMinutes(1).ToString();
}