namespace Elsa
{
    public class ServiceBusOptions
    {
        public int? NumberOfWorkers { get; set; }
        public int? MaxParallelism { get; set; }
    }
}