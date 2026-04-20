using System.Text.Json;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;

namespace Elsa.Api.Client.Resources.WorkflowDefinitions.Extensions;

/// <summary>
/// Provides extension methods for <see cref="WorkflowDefinition"/>.
/// </summary>
public static class WorkflowDefinitionExtensions
{
    private const string VariableTestValuesKey = "VariableTestValues";

    /// <summary>
    /// Gets the variable test values dictionary from the workflow definition's custom properties.
    /// If no test values exist, an empty dictionary is created and stored.
    /// </summary>
    /// <param name="workflowDefinition">The workflow definition.</param>
    /// <returns>A dictionary of variable test values keyed by variable ID.</returns>
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

    /// <summary>
    /// Gets the test value for a specific variable.
    /// </summary>
    /// <param name="workflowDefinition">The workflow definition.</param>
    /// <param name="variableId">The ID of the variable.</param>
    /// <returns>The test value, or <c>null</c> if no test value is set.</returns>
    public static object? GetVariableTestValue(this WorkflowDefinition workflowDefinition, string variableId)
    {
        var testValues = workflowDefinition.GetVariableTestValues();
        return testValues.TryGetValue(variableId, out var value) ? value : null;
    }

    /// <summary>
    /// Sets the test value for a specific variable.
    /// </summary>
    /// <param name="workflowDefinition">The workflow definition.</param>
    /// <param name="variableId">The ID of the variable.</param>
    /// <param name="value">The test value to set.</param>
    public static void SetVariableTestValue(this WorkflowDefinition workflowDefinition, string variableId, object? value)
    {
        var testValues = workflowDefinition.GetVariableTestValues();
        testValues[variableId] = value;
        workflowDefinition.CustomProperties[VariableTestValuesKey] = testValues;
    }

    /// <summary>
    /// Clears the test value for a specific variable.
    /// </summary>
    /// <param name="workflowDefinition">The workflow definition.</param>
    /// <param name="variableId">The ID of the variable whose test value should be removed.</param>
    public static void ClearVariableTestValue(this WorkflowDefinition workflowDefinition, string variableId)
    {
        var testValues = workflowDefinition.GetVariableTestValues();
        testValues.Remove(variableId);
        workflowDefinition.CustomProperties[VariableTestValuesKey] = testValues;
    }
}