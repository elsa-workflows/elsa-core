using Elsa.Workflows.Runtime.HealthChecks;
using Elsa.Workflows.Runtime.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Elsa.Extensions;

/// <summary>
/// Adds Elsa workflow runtime readiness checks to ASP.NET Core health checks.
/// </summary>
public static class HealthCheckExtensions
{
    /// <summary>
    /// Tag applied to Elsa health checks.
    /// </summary>
    public const string ElsaTag = "elsa";

    /// <summary>
    /// Tag applied to Elsa readiness checks.
    /// </summary>
    public const string ReadinessTag = "readiness";

    private static readonly string[] ReadinessTags = [ElsaTag, ReadinessTag];

    /// <summary>
    /// Adds conservative Elsa-specific readiness probes for the workflow runtime and its core stores.
    /// </summary>
    /// <param name="builder">The health checks builder.</param>
    /// <param name="includePersistence">Whether to probe workflow management/runtime stores.</param>
    /// <param name="includeDistributedLocks">Whether to probe the configured distributed lock provider.</param>
    /// <param name="configureOptions">Configures Elsa readiness probe behavior.</param>
    public static IHealthChecksBuilder AddElsaReadinessChecks(
        this IHealthChecksBuilder builder,
        bool includePersistence = true,
        bool includeDistributedLocks = false,
        Action<ElsaReadinessHealthCheckOptions>? configureOptions = null)
    {
        var optionsBuilder = builder.Services.AddOptions<ElsaReadinessHealthCheckOptions>();

        if (configureOptions != null)
            optionsBuilder.Configure(configureOptions);

        builder.AddCheck<ElsaRuntimeHealthCheck>("elsa-runtime", tags: ReadinessTags);

        if (includePersistence)
            builder.AddCheck<ElsaWorkflowPersistenceHealthCheck>("elsa-workflow-persistence", tags: ReadinessTags);

        if (includeDistributedLocks)
            builder.AddCheck<ElsaDistributedLockHealthCheck>("elsa-distributed-locks", tags: ReadinessTags);

        return builder;
    }
}
