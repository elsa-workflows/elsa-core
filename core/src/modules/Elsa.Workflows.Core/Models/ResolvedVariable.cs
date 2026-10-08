using Elsa.Workflows.Memory;

namespace Elsa.Workflows;

/// <summary>
/// Represents a variable and its value.
/// </summary>
public record ResolvedVariable(Variable Variable, object? Value);