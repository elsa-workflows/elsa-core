using Elsa.Hosting.Management.Contracts;
using Elsa.Hosting.Management.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Elsa.Hosting.Management.Services;

/// <summary>
/// Resolves the application instance name from <see cref="ApplicationInstanceOptions"/>, allowing a
/// stable name to be configured so that per-instance transport entities are reused across restarts
/// instead of accumulating. Falls back to a random name when no stable name is configured, which
/// preserves the previous default behaviour.
/// </summary>
/// <remarks>
/// Resolution order:
/// <list type="number">
/// <item><description><see cref="ApplicationInstanceOptions.InstanceName"/> when set.</description></item>
/// <item><description>The environment variable named by <see cref="ApplicationInstanceOptions.InstanceNameEnvironmentVariable"/> when configured and non-empty.</description></item>
/// <item><description>A randomly generated name (legacy behaviour).</description></item>
/// </list>
/// </remarks>
public class ConfiguredApplicationInstanceNameProvider : IApplicationInstanceNameProvider
{
    private readonly string _instanceName;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfiguredApplicationInstanceNameProvider"/> class.
    /// </summary>
    public ConfiguredApplicationInstanceNameProvider(
        IOptions<ApplicationInstanceOptions> options,
        RandomIntIdentityGenerator randomIdentityGenerator,
        ILogger<ConfiguredApplicationInstanceNameProvider> logger)
    {
        var value = options.Value;

        if (!string.IsNullOrWhiteSpace(value.InstanceName))
        {
            _instanceName = value.InstanceName.Trim();
            return;
        }

        if (!string.IsNullOrWhiteSpace(value.InstanceNameEnvironmentVariable))
        {
            var fromEnvironment = Environment.GetEnvironmentVariable(value.InstanceNameEnvironmentVariable);

            if (!string.IsNullOrWhiteSpace(fromEnvironment))
            {
                _instanceName = fromEnvironment.Trim();
                return;
            }

            logger.LogWarning(
                "The configured instance-name environment variable '{EnvironmentVariable}' is not set or empty. Falling back to a random instance name. " +
                "A random name causes per-instance transport entities (such as the Azure Service Bus change-token subscription) to be recreated on every restart, " +
                "which can accumulate until the transport's per-topic limit is reached.",
                value.InstanceNameEnvironmentVariable);
        }

        _instanceName = randomIdentityGenerator.GenerateId();
    }

    /// <inheritdoc />
    public string GetName() => _instanceName;
}
