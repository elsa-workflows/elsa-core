using System.Text.Json;
using System.Text.Json.Nodes;
using Elsa.Api.Client.Extensions;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

/// <summary>
/// Adds extension methods to <see cref="WorkflowDefinition"/>.
/// </summary>
public static class WorkflowDefinitionExtensions
{
    /// <summary>
    /// Gets the workflow context provider types that are installed on the workflow definition.
    /// </summary>
    /// <param name="workflowDefinition">The workflow definition to get the provider types from.</param>
    /// <returns>The workflow context provider types.</returns>
    public static IEnumerable<string> GetWorkflowContextProviderTypes(this WorkflowDefinition workflowDefinition)
    {
        if (!workflowDefinition.CustomProperties.TryGetValue("Elsa:WorkflowContextProviderTypes", out var providerTypesValue))
            return [];

        var jsonElement = (JsonElement)providerTypesValue;
        return jsonElement.Deserialize<string[]>(JsonSerializerOptions.Default);
    }

    /// <summary>
    /// Gets the workflow context provider types that are installed on the workflow definition.
    /// </summary>
    /// <param name="workflowDefinition">The workflow definition to get the provider types from.</param>
    /// <param name="value">The workflow context provider types.</param>
    /// <returns>The workflow context provider types.</returns>
    public static void SetWorkflowContextProviderTypes(this WorkflowDefinition workflowDefinition, IEnumerable<string> value)
    {
        var jsonElement = JsonSerializer.SerializeToElement(value.ToArray());
        workflowDefinition.CustomProperties["Elsa:WorkflowContextProviderTypes"] = jsonElement;
    }
}