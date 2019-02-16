using NodaTime;

namespace Elsa.Activities.Cron.Options
{
    public class CronOptions
    {
        public CronOptions()
        {
            SweepInterval = Period.FromMinutes(1);
        }
        
        public Period SweepInterval { get; set; }
    }
}