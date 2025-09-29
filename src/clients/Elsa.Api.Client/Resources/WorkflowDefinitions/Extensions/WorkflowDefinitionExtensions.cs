using System.Text.Json;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;

namespace Elsa.Api.Client.Resources.WorkflowDefinitions.Extensions;

public static class WorkflowDefinitionExtensions
{
    private const string VariableTestValuesKey = "VariableTestValues";

    public static IDictionary<string, object?> GetVariableTestValues(this WorkflowDefinition workflowDefinition)
    {
        if (!workflowDefinition.CustomProperties.TryGetValue(VariableTestValuesKey, out var dictionary))
        {
            var testValues = new Dictionary<string, object?>();
            workflowDefinition.CustomProperties[VariableTestValuesKey] = testValues;
            return testValues;
        }

        if (dictionary is JsonElement jsonElement)
            dictionary = JsonSerializer.Deserialize<Dictionary<string, object?>>(jsonElement.GetRawText());

        return dictionary as IDictionary<string, object?> ?? throw new InvalidOperationException("Invalid variable test values.");
    }

    public static object? GetVariableTestValue(this WorkflowDefinition workflowDefinition, string variableId)
    {
        var testValues = workflowDefinition.GetVariableTestValues();
        return testValues.TryGetValue(variableId, out var value) ? value : null;
    }

    public static void SetVariableTestValue(this WorkflowDefinition workflowDefinition, string variableId, object? value)
    {
        var testValues = workflowDefinition.GetVariableTestValues();
        testValues[variableId] = value;
        workflowDefinition.CustomProperties[VariableTestValuesKey] = testValues;
    }

    public static void ClearVariableTestValue(this WorkflowDefinition workflowDefinition, string variableId)
    {
        var testValues = workflowDefinition.GetVariableTestValues();
        testValues.Remove(variableId);
        workflowDefinition.CustomProperties[VariableTestValuesKey] = testValues;
    }
}