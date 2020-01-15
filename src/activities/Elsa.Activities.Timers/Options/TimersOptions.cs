using NodaTime;

namespace Elsa.Activities.Timers.Options
{
    public class TimersOptions
    {
        public TimersOptions()
        {
            SweepInterval = Duration.FromMinutes(1);
        }
        
        public Duration SweepInterval { get; set; }
    }
}