namespace Elsa.Connections.Services;

/// <summary>Uniform failure for unavailable, mismatched, inactive, or unauthorized connection access.</summary>
public sealed class ConnectionUnavailableException : Exception
{
    public ConnectionUnavailableException() : base("The connection is unavailable.")
    {
    }
}
