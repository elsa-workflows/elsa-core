namespace Elsa.Resilience.Options;

public class ResilienceStrategiesOptions
{
    public ICollection<IResilienceStrategy> ResilienceStrategies { get; set; } = new List<IResilienceStrategy>();
}