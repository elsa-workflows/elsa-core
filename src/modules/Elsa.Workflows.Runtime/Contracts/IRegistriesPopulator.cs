using Elsa.Expressions.Contracts;
using Elsa.Workflows.Core.Contracts;

namespace Elsa.Workflows.Runtime.Contracts;

/// <summary>
/// Updates the workflow store from <see cref="IWorkflowProvider"/> implementations, creates triggers, updates the <see cref="IActivityRegistry"/> and <see cref="IExpressionSyntaxRegistry"/>.
/// </summary>
public interface IRegistriesPopulator
{
    /// <summary>
    /// Updates the workflow store from <see cref="IWorkflowProvider"/> implementations, creates triggers, updates the <see cref="IActivityRegistry"/> and <see cref="IExpressionSyntaxRegistry"/>.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task PopulateAsync(CancellationToken cancellationToken = default);
}