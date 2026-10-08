namespace Elsa.OrchardCore.Client.Options;

/// <summary>
/// Options related to connecting to an Orchard Core tenant.
/// </summary>
public class OrchardCoreClientOptions
{
    /// <summary>
    /// The base address of the Orchard Core tenant to interact with.
    /// </summary>
    public Uri BaseAddress { get; set; } = null!;

    /// <summary>
    /// The client ID.
    /// </summary>
    public string ClientId { get; set; } = null!;
    
    /// <summary>
    /// The client secret.
    /// </summary>
    public string ClientSecret { get; set; } = null!;
}