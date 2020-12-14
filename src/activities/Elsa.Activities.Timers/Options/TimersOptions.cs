using Microsoft.Extensions.DependencyInjection;

using NodaTime;

namespace Elsa.Activities.Timers.Options
{
    public class TimersOptions
    {
        public IServiceCollection Services { get; }

        public TimersOptions(IServiceCollection services)
        {
            Services = services;
            SweepInterval = Duration.FromMinutes(1);
        }
        
        public Duration SweepInterval { get; set; }
    }
}