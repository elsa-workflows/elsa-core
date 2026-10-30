namespace Elsa.Server.Web;

/// <summary>
/// Represents the transport options for distributed caching.
/// </summary>
public enum DistributedCachingTransport
{
    None,
    Memory,
    MassTransit,
    ProtoActor
}