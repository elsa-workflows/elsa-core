using System.Text.Json;
using Microsoft.SemanticKernel;

namespace Elsa.SemanticKernel;

public static class FunctionResultExtensions
{
    public static async Task<JsonElement> AsJsonElementAsync(this Task<ExecuteFunctionResult> resultTask)
    {
        var result = await resultTask;
        return result.FunctionResult.AsJsonElement();
    }
    
    public static async Task<JsonElement> AsJsonElementAsync(this Task<FunctionResult> resultTask)
    {
        var result = await resultTask;
        return result.AsJsonElement();
    }
    
    public static JsonElement AsJsonElement(this FunctionResult result)
    {
        var response = result.GetValue<string>()!;
        return JsonSerializer.Deserialize<JsonElement>(response);
    }
}