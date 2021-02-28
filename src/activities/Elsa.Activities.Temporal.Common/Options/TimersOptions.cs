using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Activities.Temporal.Options
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