using System.Text.Json.Nodes;

namespace Elsa.Studio.Workflows.UI.Args;

/// <summary>
/// Represents the arguments for the <c>ActivityEmbeddedPortSelected</c> event.
/// </summary>
/// <param name="Activity">The activity that contains the port.</param>
/// <param name="PortName">The name of the port.</param>
public record ActivityEmbeddedPortSelectedArgs(JsonObject Activity, string PortName);