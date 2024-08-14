namespace Elsa.OrchardCore.Client;

/// Options related to connecting to an Orchard Core tenant.
public class OrchardCoreClientOptions
{
    /// The base address of the Orchard Core tenant to interact with.
    public Uri BaseAddress { get; set; } = default!;

    /// The client ID.
    public string ClientId { get; set; } = default!;
    
    /// The client secret.
    public string ClientSecret { get; set; } = default!;
}