using Elsa.Environments.Contracts;
using Elsa.Environments.Models;
using Elsa.Environments.Options;
using Microsoft.Extensions.Options;

namespace Elsa.Environments.Providers;

/// <summary>
/// An implementation of <see cref="IEnvironmentsProvider"/> that reads environment definitions from configuration.
/// </summary>
public class ConfigurationEnvironmentsProvider : IEnvironmentsProvider
{
    private readonly IOptions<EnvironmentsOptions> _options;

    /// <summary>
    /// Initializes a new instance of <see cref="ConfigurationEnvironmentsProvider"/>.
    /// </summary>
    public ConfigurationEnvironmentsProvider(IOptions<EnvironmentsOptions> options)
    {
        _options = options;
    }
    
    /// <inheritdoc />
    public ValueTask<IEnumerable<ServerEnvironment>> GetEnvironmentsAsync(CancellationToken cancellationToken = default)
    {
        return new (_options.Value.Environments);
    }
}