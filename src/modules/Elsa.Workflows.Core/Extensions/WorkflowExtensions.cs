using System.Text.Json;
using Elsa.Expressions.Helpers;
using Elsa.Workflows;
using Elsa.Workflows.Activities;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

/// <summary>
/// Provides extension methods for <see cref="Workflow"/>.
/// </summary>  
public static partial class WorkflowExtensions
{
    extension(Workflow workflow)
    {
        /// <summary>
        /// Returns a boolean indicating whether the workflow was created with modern tooling.
        /// </summary>
        public bool CreatedWithModernTooling() => workflow.WorkflowMetadata.ToolVersion?.Major >= 3;

        /// <summary>
        /// Executes the specified action depending on whether the workflow was created with modern tooling or not.
        /// </summary>
        public void WhenCreatedWithModernTooling(Action modernToolingAction, Action legacyToolingAction)
        {
            if (workflow.CreatedWithModernTooling())
                modernToolingAction();
            else
                legacyToolingAction();
        }

        /// <summary>
        /// Sets a test variable value in the workflow's custom properties, which can be accessed during test execution by <see cref="IActivityTestRunner"/>.
        /// </summary>
        /// <typeparam name="T">The type of the variable value.</typeparam>
        /// <param name="variableName">The name of the variable to set.</param>
        /// <param name="value">The value to assign to the variable.</param>
        public void SetTestVariable<T>(string variableName, T? value)
        {
            workflow.SetTestProperty(ActivityTestRunner.VariableTestValuesPropertyName, variableName, value);
        }

        /// <summary>
        /// Sets a named value in a dictionary stored within the workflow's custom properties.
        /// </summary>
        /// <typeparam name="T">The type of the value to store.</typeparam>
        /// <param name="collectionName">The key under which the dictionary is stored in custom properties.</param>
        /// <param name="name">The key within the dictionary to set.</param>
        /// <param name="value">The value to store.</param>
        internal void SetTestProperty<T>(string collectionName, string name, T value)
        {
            if (!workflow.CustomProperties.TryGetValue<Dictionary<string, object?>>(collectionName, out Dictionary<string, object?>? collection))
            {
                collection = new Dictionary<string, object?>();
                workflow.CustomProperties[collectionName] = collection;
            }
            collection[name] = value!;
        }

        /// <summary>
        /// Gets all the test variables that were defined
        /// </summary>
        public Dictionary<string, object?> GetTestVariables()
        {
            var customProperties = workflow.CustomProperties;
            var hasValues = customProperties.TryGetValue(ActivityTestRunner.VariableTestValuesPropertyName, out var values);
            Dictionary<string, object?> variables = new Dictionary<string, object?>();

            if (!hasValues)
            {
                return variables;
            }

            switch (values)
            {
                // ExpandoObject: Handled by enumerable 
                case IEnumerable<KeyValuePair<string, object?>> enumerable:
                    variables = new Dictionary<string, object?>(enumerable);
                    break;
                case JsonElement jsonElement:
                    if (JsonSerializer.Deserialize<Dictionary<string, object?>>(jsonElement.GetRawText()) is Dictionary<string, object?> jsonValues)
                    {
                        variables = jsonValues;
                    }
                    break;
            }

            var workflowVariables = workflow.Variables
                .ToDictionary(x => x.Id, x => x);

            variables.RemoveWhere(x => !workflowVariables.ContainsKey(x.Key));

            return variables
                .ToDictionary(x => x.Key, x =>
                {
                    var variableDefinition = workflowVariables[x.Key];
                    var variableType = variableDefinition.GetVariableType();
                    return x.Value.ConvertTo(variableType);
                });
        }
    }
}