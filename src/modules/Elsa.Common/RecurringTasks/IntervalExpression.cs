namespace Elsa.Common.RecurringTasks;

public class IntervalExpression
{
    public static IntervalExpression FromInterval(TimeSpan interval)
    {
        return new IntervalExpression
        {
            Type = IntervalExpressionType.Interval,
            Expression = interval.ToString()
        };
    }
    
    public static IntervalExpression FromCron(string cronExpression)
    {
        return new IntervalExpression
        {
            Type = IntervalExpressionType.Cron,
            Expression = cronExpression
        };
    }
    
    public IntervalExpressionType Type { get; set; }
    public string Expression { get; set; } = default!;
}