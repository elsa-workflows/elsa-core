using System.Text.Json;
using Elsa.Expressions.Models;
using Elsa.Extensions;

namespace Elsa.Workflows.UIHints.Dictionary;

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
                {
                    // Not a JSON object, so just use the value as-is.
                    tempDictionary[dict.Key] = dict.Value;
                    continue;
                }

                // JSON object, so extract the type and value properties.
                json.TryGetProperty("type", out var typeProperty);
                json.TryGetProperty("value", out var valueProperty);

                // Evaluate the expression.
                var expression = new Expression(typeProperty.ToString(), valueProperty.ToString());
                var val = await evaluator.EvaluateAsync<string>(expression, expressionExecutionContext);

                // Add the evaluated value to the dictionary.
                tempDictionary[dict.Key] = val;
            }

            // Replace the original dictionary with the evaluated one.
            value = tempDictionary;
        }

        return value;
    }
}