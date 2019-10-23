using NodaTime;

namespace Elsa.Activities.Timers.Options
{
    public class TimersOptions
    {
        public TimersOptions()
        {
            SweepInterval = Period.FromMinutes(1);
        }
        
        public Period SweepInterval { get; set; }
    }
}