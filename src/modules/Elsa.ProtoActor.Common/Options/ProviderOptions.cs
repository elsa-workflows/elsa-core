namespace Elsa.ProtoActor.Common.Options;

public class ProviderOptions
{
    public string Name { get; set; } = "elsa-cluster";

    public bool WithDeveloperLogging { get; set; } = true;
    
    public bool WithMetrics { get; set; }
}
