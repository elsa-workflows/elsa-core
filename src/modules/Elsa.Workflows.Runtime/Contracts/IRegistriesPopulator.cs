using Elsa.Expressions.Contracts;

namespace Elsa.Workflows.Runtime;

/// <summary>
/// Updates the workflow store from <see cref="IWorkflowsProvider"/> implementations, creates triggers, updates the <see cref="IActivityRegistry"/> and <see cref="IExpressionDescriptorRegistry"/>.
/// </summary>
public interface IRegistriesPopulator
{
    /// <summary>
    /// Updates the workflow store from <see cref="IWorkflowsProvider"/> implementations, creates triggers, updates the <see cref="IActivityRegistry"/> and <see cref="IExpressionDescriptorRegistry"/>.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task PopulateAsync(CancellationToken cancellationToken = default);
}