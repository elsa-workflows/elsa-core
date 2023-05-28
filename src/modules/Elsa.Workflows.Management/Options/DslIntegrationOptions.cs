using Elsa.Dsl.Models;

namespace Elsa.Workflows.Management.Options;

/// <summary>
/// Options for the DSL integration.
/// </summary>
public class DslIntegrationOptions
{
    /// <summary>
    /// A collection of function activity descriptors that are available to the DSL.
    /// </summary>
    public IDictionary<string, FunctionActivityDescriptor> FunctionActivityDescriptors { get; set; } = new Dictionary<string, FunctionActivityDescriptor>();
}