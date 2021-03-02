// ReSharper disable once CheckNamespace
namespace NodaTime
{
    public static class DurationExtensions
    {
        public static string ToCronExpression(this Duration duration)
        {
            static string CreateCronComponent(int number)
            {
                return (number > 0 ? $"*/{number}" : "* ");
            }

            var cron = CreateCronComponent(duration.Seconds);
            cron += ' ' + CreateCronComponent(duration.Minutes);
            cron += ' ' + CreateCronComponent(duration.Hours);
            cron += ' ' + CreateCronComponent(duration.Days);
            return cron + " * *";
        }
    }
}
