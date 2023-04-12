namespace Elsa.Hangfire.Extensions;

/// <summary>
/// Adds extension methods to <see cref="TimeSpan"/> that converts it to a cron expression.
/// </summary>
public static class TimeSpanExtensions
{
    /// <summary>
    /// Converts the specified time span to a cron expression.
    /// </summary>
    /// <param name="timeSpan">The time span.</param>
    /// <returns>The cron expression.</returns>
    public static string ToCronExpression(this TimeSpan timeSpan)
    {
        static string CreateCronComponent(int number) => (number > 0 ? $"*/{number}" : "*");

        var cron = CreateCronComponent(timeSpan.Seconds);
        cron += ' ' + CreateCronComponent(timeSpan.Minutes);
        cron += ' ' + CreateCronComponent(timeSpan.Hours);
        cron += ' ' + CreateCronComponent(timeSpan.Days);
        return cron + " * *";
    }
}