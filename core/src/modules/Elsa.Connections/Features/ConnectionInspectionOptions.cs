namespace Elsa.Connections.Features;

/// <summary>The integration environment selected by the host for human metadata inspection.</summary>
public sealed class ConnectionInspectionOptions
{
    public string EnvironmentId { get; set; } = string.Empty;
}
