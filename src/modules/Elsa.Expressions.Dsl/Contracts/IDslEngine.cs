using Elsa.Workflows.Activities;

namespace Elsa.Expressions.Dsl.Contracts;

/// <summary>
/// Represents a DSL engine that parse a workflow definition script into a <see cref="Workflow"/> instance.
/// </summary>
public interface IDslEngine
{
    /// <summary>
    /// Parses the specified script into a <see cref="Workflow"/> instance.
    /// </summary>
    Task<Workflow> ParseAsync(string script, CancellationToken cancellationToken = default);
}