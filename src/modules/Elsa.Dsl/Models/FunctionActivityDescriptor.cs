using Elsa.Workflows.Core.Contracts;

namespace Elsa.Dsl.Models;

/// <summary>
/// Describes a function that is mapped to an activity that can be invoked from a DSL script.
/// </summary>
/// <param name="FunctionName">The name of the function.</param>
/// <param name="ActivityTypeName">The name of the activity type.</param>
/// <param name="PropertyNames">The names of the properties that are mapped to the function arguments.</param>
/// <param name="Configure">An optional action that can be used to configure the activity.</param>
public record FunctionActivityDescriptor(string FunctionName, string ActivityTypeName, IEnumerable<string>? PropertyNames = default, Action<IActivity>? Configure = default);