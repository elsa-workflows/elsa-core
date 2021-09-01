using Elsa.Options;
using Rebus.Config;

namespace Elsa
{
    public static class RebusOptionsConfigurerExtensions
    {
        public static void Apply(this OptionsConfigurer configurer, ServiceBusOptions options)
        {
            if(options.NumberOfWorkers != null)
                configurer.SetNumberOfWorkers(options.NumberOfWorkers.Value);
                    
            if(options.MaxParallelism != null)
                configurer.SetMaxParallelism(options.MaxParallelism.Value);
        }
    }
}