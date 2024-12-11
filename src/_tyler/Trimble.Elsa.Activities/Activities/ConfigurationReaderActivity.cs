using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Memory;
using System.Reflection;

namespace Trimble.Elsa.Activities.Activities;

/// <summary>
/// Obtains the templated type from the DI container and projects its properties 
/// onto variables named according to the Microsoft environment variable naming 
/// conventions that match the structure of appsettings.
///
/// (https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/?view=aspnetcore-8.0#naming-of-environment-variables)
/// </summary>
[Activity(
    "Trimble.Elsa.Activities.Activities",
    "ServiceRegistry",
    "Reads an IConfiguration system object and maps it to variables.",
    DisplayName = "Configuration Mapper",
    Kind = ActivityKind.Task)]
public class ConfigurationReaderActivity<T> : CodeActivity<T> where T : notnull
{
    /// <summary>
    /// The variables resulting from parsing the configuration object.
    /// </summary>
    public List<Variable> Variables { get; set; } = [];

    /// <inheritdoc/>
    protected override void Execute(ActivityExecutionContext context)
    {
        T injectedService = context.GetRequiredService<T>();

        var variablesToSet = ProjectConfigOntoVariables(injectedService);
        foreach (var variable in variablesToSet)
        {
            var encrypted = context.EncryptIfNeeded(variable.Name, variable.Value);
            var newVar = context.SetVariable(variable.Name, encrypted);
            Variables.Add(newVar);
        }
    }

    /// <summary>
    /// Connects an Elsa variable name to its value.
    /// </summary>
    protected record ConfigVariableMap
    {
        /// <summary>
        /// The name of the target Elsa variable.
        /// </summary>
        public required string Name { get; set; }

        /// <summary>
        /// Values are confined to string types for now to simplify encryption.
        /// </summary>
        public required string? Value { get; set; }
    }

    /// <summary>
    /// The list of variable names for this configuration.
    /// </summary>
    public static List<string> ConfigurationVariableNames()
        => ConfigurationVariableNames(typeof(T));

    /// <summary>
    /// The list of variable names for this configuration.
    /// </summary>
    private static List<string> ConfigurationVariableNames(Type type, string prefix = "")
    {
        var variables = new List<string>();
        if (prefix == string.Empty)
        {
            prefix = $"{type.Name}__";
        }

        PropertyInfo[] properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        foreach (var property in properties)
        {
            string propertyName = $"{prefix}{property.Name}";
            if (property.PropertyType.IsClass && property.PropertyType != typeof(string))
            {
                variables.AddRange(ConfigurationVariableNames(property.PropertyType, $"{propertyName}__"));
            }
            else
            {
                variables.Add(propertyName);
            }
        }

        return variables;
    }

    /// <summary>
    /// Maps configuration values to variable names.
    /// <summary>
    protected static List<ConfigVariableMap> ProjectConfigOntoVariables(
        object obj,
        string prefix = "")
    {
        List<ConfigVariableMap> variables = [];
        if (string.IsNullOrWhiteSpace(prefix))
        {
            prefix = $"{obj.GetType().Name}__";
        }

        PropertyInfo[] properties = obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
        foreach (var property in properties)
        {
            string propertyName = $"{prefix}{property.Name}";
            if (property.PropertyType.IsClass && property.PropertyType != typeof(string))
            {
                var nestedObject = property.GetValue(obj);
                if (nestedObject != null)
                {
                    variables.AddRange(ProjectConfigOntoVariables(nestedObject, $"{propertyName}__"));
                }
            }
            else
            {
                variables.Add(new() { Name = propertyName, Value = property.GetValue(obj)?.ToString() });
            }
        }

        return variables;
    }
}
