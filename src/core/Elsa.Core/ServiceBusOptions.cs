using Rebus.Config;

namespace Elsa
{
    public class ServiceBusOptions
    {
        public int? NumberOfWorkers { get; set; }
        public int? MaxParallelism { get; set; }
        public string? QueuePrefix { get; set; }

        public void Apply(OptionsConfigurer configurer)
        {
            if(NumberOfWorkers != null)
                configurer.SetNumberOfWorkers(NumberOfWorkers.Value);
                    
            if(MaxParallelism != null)
                configurer.SetMaxParallelism(MaxParallelism.Value);
        }
    }
}