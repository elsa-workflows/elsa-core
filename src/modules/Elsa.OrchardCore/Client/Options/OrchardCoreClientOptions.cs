namespace Elsa.OrchardCore.Client;

/// <summary>
/// Options related to connecting to an Orchard Core tenant.
/// </summary>
public class OrchardCoreClientOptions
{
    /// <summary>
    /// The base address of the Orchard Core tenant to interact with.
    /// </summary>
    public Uri BaseAddress { get; set; } = default!;

    /// <summary>
    /// The client ID.
    /// </summary>
    public string ClientId { get; set; } = default!;
    
    /// <summary>
    /// The client secret.
    /// </summary>
    public string ClientSecret { get; set; } = default!;
}