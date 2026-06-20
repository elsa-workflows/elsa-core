using Elsa.Hosting.Management.Contracts;
using Elsa.Hosting.Management.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;

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
    internal const int AzureServiceBusSubscriptionNameMaxLength = 50;
    internal const string TriggerChangeTokenSignalEndpointNameSuffix = "-elsa-tct";
    private const int ShortenedNameHashLength = 16;
    internal static readonly int ConfiguredInstanceNameMaxLength = AzureServiceBusSubscriptionNameMaxLength - TriggerChangeTokenSignalEndpointNameSuffix.Length;

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
            _instanceName = ResolveConfiguredInstanceName(value.InstanceName, $"{nameof(ApplicationInstanceOptions)}.{nameof(ApplicationInstanceOptions.InstanceName)}", logger);
            return;
        }

        if (!string.IsNullOrWhiteSpace(value.InstanceNameEnvironmentVariable))
        {
            var environmentVariable = value.InstanceNameEnvironmentVariable.Trim();
            var fromEnvironment = Environment.GetEnvironmentVariable(environmentVariable);

            if (!string.IsNullOrWhiteSpace(fromEnvironment))
            {
                _instanceName = ResolveConfiguredInstanceName(fromEnvironment, $"environment variable '{environmentVariable}'", logger);
                return;
            }

            logger.LogWarning(
                "The configured instance-name environment variable '{EnvironmentVariable}' is not set or empty. Falling back to a random instance name. " +
                "A random name causes per-instance transport entities (such as the Azure Service Bus change-token subscription) to be recreated on every restart, " +
                "which can accumulate until the transport's per-topic limit is reached.",
                environmentVariable);
        }

        _instanceName = randomIdentityGenerator.GenerateId();
    }

    /// <inheritdoc />
    public string GetName() => _instanceName;

    private static string ResolveConfiguredInstanceName(string value, string source, ILogger logger)
    {
        var instanceName = value.Trim();

        if (!IsValidConfiguredInstanceName(instanceName))
            throw new InvalidOperationException(
                $"The configured application instance name from {source} contains invalid characters. " +
                "Use only letters, numbers, periods, hyphens, or underscores, and start and end the value with a letter or number.");

        if (instanceName.Length <= ConfiguredInstanceNameMaxLength)
            return instanceName;

        var shortenedName = ShortenConfiguredInstanceName(instanceName);

        logger.LogWarning(
            "The configured application instance name from {Source} is {Length} characters long, exceeding the {MaxLength}-character limit required for Azure Service Bus transport entities. " +
            "Using deterministic shortened instance name '{ShortenedName}' instead.",
            source,
            instanceName.Length,
            ConfiguredInstanceNameMaxLength,
            shortenedName);

        return shortenedName;
    }

    private static bool IsValidConfiguredInstanceName(string instanceName)
    {
        return IsAsciiLetterOrDigit(instanceName[0])
            && IsAsciiLetterOrDigit(instanceName[^1])
            && instanceName.All(c => IsAsciiLetterOrDigit(c) || c is '.' or '-' or '_');
    }

    private static bool IsAsciiLetterOrDigit(char value) =>
        value is >= 'a' and <= 'z' or >= 'A' and <= 'Z' or >= '0' and <= '9';

    private static string ShortenConfiguredInstanceName(string instanceName)
    {
        var hash = Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(instanceName))).ToLowerInvariant()[..ShortenedNameHashLength];
        var prefixLength = ConfiguredInstanceNameMaxLength - hash.Length - 1;
        var prefix = instanceName[..prefixLength];

        return $"{prefix}-{hash}";
    }
}
