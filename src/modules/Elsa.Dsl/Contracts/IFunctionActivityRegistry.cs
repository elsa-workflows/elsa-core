using Elsa.Dsl.Models;
using Elsa.Workflows.Core.Contracts;

namespace Elsa.Dsl.Contracts;

/// <summary>
/// Provides a registry for mapping functions to activities that can be invoked from a DSL script.
/// </summary>
public interface IFunctionActivityRegistry
{
    /// <summary>
    /// Registers a function that is mapped to an activity that can be invoked from a DSL script.
    /// </summary>
    /// <param name="functionName">The name of the function.</param>
    /// <param name="activityTypeName">The name of the activity type.</param>
    /// <param name="propertyNames">The names of the properties that are mapped to the function arguments.</param>
    /// <param name="configure">An optional action that can be used to configure the activity.</param>
    void RegisterFunction(string functionName, string activityTypeName, IEnumerable<string>? propertyNames = default, Action<IActivity>? configure = default);

    /// <summary>
    /// Registers a function that is mapped to an activity that can be invoked from a DSL script.
    /// </summary>
    /// <param name="descriptor">The descriptor that describes the function.</param>
    void RegisterFunction(FunctionActivityDescriptor descriptor);
    
    /// <summary>
    /// Resolves a function to an activity that can be invoked from a DSL script.
    /// </summary>
    /// <param name="functionName">The name of the function.</param>
    /// <param name="arguments">The arguments that are passed to the function.</param>
    /// <returns>An activity that can be invoked from a DSL script.</returns>
    IActivity ResolveFunction(string functionName, IEnumerable<object?>? arguments = default);
}