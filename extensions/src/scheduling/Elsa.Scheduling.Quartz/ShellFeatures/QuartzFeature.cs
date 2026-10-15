using CShells.Features;
using CShells.Lifecycle;
using Elsa.Platform.PackageManifest.Generator.Hints;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Quartz;

namespace Elsa.Scheduling.Quartz.ShellFeatures;

/// <summary>
/// Represents a feature that enables Quartz.NET for background job scheduling.
/// </summary>
[ShellFeature(
    DisplayName = "Quartz Scheduler",
    Description = "Enables Quartz.NET for background job scheduling")]
[UsedImplicitly]
public class QuartzFeature : IShellFeature, IPostConfigureShellServices
{
    /// <summary>
    /// Optional delay before the scheduler begins processing jobs after shell activation.
    /// </summary>
    [ManifestSetting(
        DisplayName = "Start delay",
        Description = "Optional delay before the scheduler begins processing jobs after shell activation.",
        Category = "Scheduler",
        Advanced = true,
        RestartRequired = true)]
    public TimeSpan? StartDelay { get; set; }

    /// <summary>
    /// Whether to wait for running jobs to complete before the scheduler shuts down on
    /// shell deactivation. Defaults to <c>true</c>.
    /// </summary>
    [ManifestSetting(
        DisplayName = "Wait for jobs to complete",
        Description = "Wait for running jobs to complete before the scheduler shuts down on shell deactivation.",
        Category = "Scheduler",
        RestartRequired = true)]
    public bool WaitForJobsToComplete { get; set; } = true;

    /// <summary>
    /// The Quartz scheduler instance ID. Use <c>"AUTO"</c> (default) for automatic
    /// generation, which is required for clustering.
    /// </summary>
    [ManifestSetting(
        DisplayName = "Scheduler ID",
        Description = "The Quartz scheduler instance ID. Use AUTO for automatic generation, which is required for clustering.",
        Category = "Scheduler",
        RestartRequired = true)]
    public string SchedulerId { get; set; } = "AUTO";

    /// <summary>The Quartz scheduler name. Defaults to <c>"ElsaScheduler"</c>.</summary>
    [ManifestSetting(
        DisplayName = "Scheduler name",
        Description = "The Quartz scheduler name.",
        Category = "Scheduler",
        RestartRequired = true)]
    public string SchedulerName { get; set; } = "ElsaScheduler";

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddQuartz(quartz =>
        {
            quartz.UseDefaultThreadPool();
            quartz.SchedulerId = SchedulerId;
            quartz.SchedulerName = SchedulerName;
        });

        // QuartzShellLifecycleHandler owns start/stop — no AddQuartzHostedService needed.
        // The Quartz hosted service is only invoked by the root IHost, which never sees
        // shell-scoped registrations, so registering it would have no effect.
        services.AddSingleton<QuartzShellLifecycleHandler>(sp =>
        {
            return new(
                sp.GetRequiredService<ISchedulerFactory>(),
                sp.GetRequiredService<Microsoft.Extensions.Hosting.IHostApplicationLifetime>(),
                sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<QuartzShellLifecycleHandler>>())
            {
                StartDelay = StartDelay,
                WaitForJobsToComplete = WaitForJobsToComplete,
            };
        });

        services.AddSingleton<IDrainHandler>(sp => sp.GetRequiredService<QuartzShellLifecycleHandler>());
    }

    public void PostConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<IShellInitializer>(sp => sp.GetRequiredService<QuartzShellLifecycleHandler>());
    }
}
