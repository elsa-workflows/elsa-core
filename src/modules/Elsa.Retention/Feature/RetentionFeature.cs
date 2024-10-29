using Elsa.Extensions;
using Elsa.Features.Abstractions;
using Elsa.Features.Services;
using Elsa.Retention.CleanupStrategies;
using Elsa.Retention.Collectors;
using Elsa.Retention.Contracts;
using Elsa.Retention.Extensions;
using Elsa.Retention.Jobs;
using Elsa.Retention.Options;
using Elsa.Workflows.Runtime.Entities;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Retention.Feature;

/// <summary>
///     The retention features provides automated cleanup of workflow instances
/// </summary>
public class RetentionFeature : FeatureBase
{
    /// <summary>
    ///     Create the retention feature
    /// </summary>
    /// <param name="module"></param>
    public RetentionFeature(IModule module) : base(module)
    {
    }

    /// <summary>
    ///     Gets or sets a delegate to configure the retention options.
    /// </summary>
    public Action<CleanupOptions> ConfigureCleanupOptions { get; set; } = _ => { };

    /// <inheritdoc cref="FeatureBase" />
    public override void Apply()
    {
        Services.Configure(ConfigureCleanupOptions);
        Services.AddTransient<CleanupJob>();

        Services.AddScoped<IDeletionCleanupStrategy<StoredBookmark>, DeleteBookmarkStrategy>();
        Services.AddScoped<IDeletionCleanupStrategy<ActivityExecutionRecord>, DeleteActivityExecutionRecordStrategy>();
        Services.AddScoped<IDeletionCleanupStrategy<WorkflowExecutionLogRecord>, DeleteWorkflowExecutionRecordStrategy>();

        Services.AddScoped<IRelatedEntityCollector, BookmarkCollector>();
        Services.AddScoped<IRelatedEntityCollector, ActivityExecutionRecordCollector>();
        Services.AddScoped<IRelatedEntityCollector, WorkflowExecutionLogRecordCollector>();

        Services.AddRecurringTask<CleanupRecurringTask>(TimeSpan.FromHours(4));

        foreach (var policy in this.GetPolicies())
        {
            Services.AddSingleton(policy);
        }
    }
}