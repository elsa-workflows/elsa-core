using Elsa.Environments.Contracts;
using Elsa.Environments.Models;
using Elsa.Environments.Options;
using Microsoft.Extensions.Options;

namespace Elsa.Environments.Services;

/// <inheritdoc />
public class DefaultEnvironmentsManager : IEnvironmentsManager
{
    private readonly IEnumerable<IEnvironmentsProvider> _providers;
    private readonly IOptions<EnvironmentsOptions> _options;

    /// <summary>
    /// Initializes a new instance of <see cref="DefaultEnvironmentsManager"/>.
    /// </summary>
    public DefaultEnvironmentsManager(IEnumerable<IEnvironmentsProvider> providers, IOptions<EnvironmentsOptions> options)
    {
        _providers = providers;
        _options = options;
    }

    /// <inheritdoc />
    public async ValueTask<IEnumerable<ServerEnvironment>> ListEnvironmentsAsync(CancellationToken cancellationToken = default)
    {
        var allEnvironments = new List<ServerEnvironment>();

        foreach (var provider in _providers)
        {
            var environments = await provider.GetEnvironmentsAsync(cancellationToken);
            allEnvironments.AddRange(environments);
        }

        return allEnvironments;
    }

    /// <inheritdoc />
    public ValueTask<string?> GetDefaultEnvironmentNameAsync(CancellationToken cancellationToken = default)
    {
        return new(_options.Value.DefaultEnvironmentName);
    }
}