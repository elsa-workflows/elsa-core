using System.Text.Json.Nodes;
using Elsa.Api.Client.Shared.Models;

namespace Elsa.Api.Client.Resources.WorkflowDefinitions.Models;

public class WorkflowSubgraph
{
    public string NodeId { get; set; }
    public JsonObject Activity { get; set; } = default!;
    public ICollection<ActivityNode> Children { get; set; } = default!;
}