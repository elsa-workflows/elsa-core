namespace System
{
    public static class TimeSpanExtensions
    {
        public static string ToCronExpression(this TimeSpan timeSpan)
        {
            string CreateCronComponent(int number)
            {
                return '*' + (timeSpan.Seconds > 0 ? $"*/{number}" : string.Empty);
            }

            var cron = CreateCronComponent(timeSpan.Seconds);
            cron += ' ' + CreateCronComponent(timeSpan.Minutes);
            cron += ' ' + CreateCronComponent(timeSpan.Hours);
            cron += ' ' + CreateCronComponent(timeSpan.Days);
            return cron + " * *";
        }
    }
}
