using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Activities.Temporal.Common.Options
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