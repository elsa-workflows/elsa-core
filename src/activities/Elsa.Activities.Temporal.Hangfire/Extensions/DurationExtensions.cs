using NodaTime;

namespace Elsa.Activities.Temporal.Hangfire.Extensions
{
    public static class DurationExtensions
    {
        public static string ToCronExpression(this Duration duration)
        {
            static string CreateCronComponent(int number) => (number > 0 ? $"*/{number}" : "*");

            var cron = CreateCronComponent(duration.Seconds);
            cron += ' ' + CreateCronComponent(duration.Minutes);
            cron += ' ' + CreateCronComponent(duration.Hours);
            cron += ' ' + CreateCronComponent(duration.Days);
            return cron + " * *";
        }
    }
}
