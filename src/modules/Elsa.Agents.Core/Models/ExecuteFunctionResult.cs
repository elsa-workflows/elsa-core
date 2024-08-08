using System.Text.Json;
using Microsoft.SemanticKernel;

namespace Elsa.Agents;

public record ExecuteFunctionResult(FunctionConfig Function, FunctionResult FunctionResult)
{
    public object? ParseResult()
    {
        var targetType = Type.GetType(Function.OutputVariable.Type) ?? typeof(JsonElement);
        var json = FunctionResult.GetValue<string>();
        return JsonSerializer.Deserialize(json, targetType);
    }
}