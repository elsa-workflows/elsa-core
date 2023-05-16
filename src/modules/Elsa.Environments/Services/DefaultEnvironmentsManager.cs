using Elsa.Environments.Contracts;
using Elsa.Environments.Models;

namespace Elsa.Environments.Services;

/// <inheritdoc />
public class DefaultEnvironmentsManager : IEnvironmentsManager
{
    private readonly IEnumerable<IEnvironmentsProvider> _providers;

    /// <summary>
    /// Initializes a new instance of <see cref="DefaultEnvironmentsManager"/>.
    /// </summary>
    public DefaultEnvironmentsManager(IEnumerable<IEnvironmentsProvider> providers)
    {
        _providers = providers;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<WorkflowsEnvironment>> ListEnvironmentsAsync(CancellationToken cancellationToken = default)
    {
        var allEnvironments = new List<WorkflowsEnvironment>();

        foreach (var provider in _providers)
        {
            var environments = await provider.GetEnvironmentsAsync(cancellationToken);
            allEnvironments.AddRange(environments);
        }

        return allEnvironments;
    }
}