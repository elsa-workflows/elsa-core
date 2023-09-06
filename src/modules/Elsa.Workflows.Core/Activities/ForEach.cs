using Elsa.Workflows.Core.Attributes;

namespace Elsa.Workflows.Core.Activities;

/// <summary>
/// Iterate over a set of values.
/// </summary>
[Activity("Elsa", "Looping", "Iterate over a set of values.")]
public class ForEach : ForEach<object>
{
}