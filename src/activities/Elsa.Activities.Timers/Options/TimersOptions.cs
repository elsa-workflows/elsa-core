using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Activities.Timers.Options
{
    public class TimersOptions
    {
        public TimersOptions(IServiceCollection services)
        {
            Services = services;
        }

        public IServiceCollection Services { get; }

    }
}