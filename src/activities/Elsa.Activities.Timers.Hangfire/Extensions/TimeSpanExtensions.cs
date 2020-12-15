namespace System
{
    public static class TimeSpanExtensions
    {
        public static string ToCronExpression(this TimeSpan timeSpan)
        {
            string CreateCronComponent(int number)
            {
                return (number > 0 ? $"*/{number}" : "* ");
            }

            var cron = CreateCronComponent(timeSpan.Seconds);
            cron += ' ' + CreateCronComponent(timeSpan.Minutes);
            cron += ' ' + CreateCronComponent(timeSpan.Hours);
            cron += ' ' + CreateCronComponent(timeSpan.Days);
            return cron + " * *";
        }
    }
}
