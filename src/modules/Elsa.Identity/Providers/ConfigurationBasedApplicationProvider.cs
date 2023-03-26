using Elsa.Identity.Contracts;
using Elsa.Identity.Entities;
using Elsa.Identity.Models;
using Elsa.Identity.Options;
using JetBrains.Annotations;
using Microsoft.Extensions.Options;

namespace Elsa.Identity.Providers;

/// <summary>
/// Represents an application provider that uses <see cref="ApplicationsOptions"/> to find applications.
/// </summary>
[PublicAPI]
public class ConfigurationBasedApplicationProvider : IApplicationProvider
{
    private readonly IOptions<ApplicationsOptions> _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationBasedApplicationProvider"/> class.
    /// </summary>
    public ConfigurationBasedApplicationProvider(IOptions<ApplicationsOptions> options)
    {
        _options = options;
    }

    /// <inheritdoc />
    public Task<Application?> FindAsync(ApplicationFilter filter, CancellationToken cancellationToken = default)
    {
        var applicationsQueryable = _options.Value.Applications.AsQueryable();
        var application = filter.Apply(applicationsQueryable).FirstOrDefault();
        return Task.FromResult(application);
    }
}