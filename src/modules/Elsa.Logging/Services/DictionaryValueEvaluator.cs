using System.Text.Json;
using Elsa.Expressions.Models;
using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.UIHints;

namespace Elsa.Logging.Services;

public class DictionaryValueEvaluator : IActivityInputEvaluator
{
    public async Task<object?> EvaluateAsync(ActivityInputEvaluatorContext context)
    {
        var wrappedInput = context.Input;
        var evaluator = context.ExpressionEvaluator;
        var expressionExecutionContext = context.ExpressionExecutionContext;
        var inputDescriptor = context.InputDescriptor;
        var defaultValue = inputDescriptor.DefaultValue;
        var value = wrappedInput.Expression != null ? await evaluator.EvaluateAsync(wrappedInput, expressionExecutionContext) : defaultValue;
        if (value is IDictionary<string, object> dictionary && inputDescriptor.UIHint == InputUIHints.Dictionary)
        {
            var tempDictionary = new Dictionary<string, object?>(dictionary.Count);
            foreach (var dict in dictionary)
            {
                if (dict.Value is not JsonElement json)
                    continue;
                        
                json.TryGetProperty("type" , out var typeProperty);
                json.TryGetProperty("value" , out var valueProperty);
                        
                var expression = new Expression(typeProperty.ToString(), valueProperty.ToString());
                var val = await evaluator.EvaluateAsync<string>(expression, expressionExecutionContext);
                tempDictionary[dict.Key] = val;
            }
            value = tempDictionary;
        }
        
        return value;
    }
}