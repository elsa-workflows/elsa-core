using System.Text.Json;
using System.Text.Json.Nodes;
using Elsa.Api.Client.Resources.Scripting.Models;
using Elsa.Api.Client.Serialization;

namespace Elsa.Api.Client.Resources.ResilienceStrategies.Models;

public class ResilienceStrategyConfig
{
    public ResilienceStrategyConfigMode Mode { get; set; }
    public string? StrategyId { get; set; }
    public Expression? Expression { get; set; }

    public JsonNode SerializeToNode()
    {
        return JsonSerializer.SerializeToNode(this, SerializerOptions.ResilienceStrategyConfigSerializerOptions)!;
    }
    
    public static ResilienceStrategyConfig? Deserialize(JsonNode? node)
    {
        return node.Deserialize<ResilienceStrategyConfig?>(SerializerOptions.ResilienceStrategyConfigSerializerOptions)!;
    }
}