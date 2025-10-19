using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Models;
using Elsa.Workflows.Options;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Testing.Shared;

/// <summary>
/// Provides extension methods for <see cref="IServiceProvider"/>.
/// </summary>
[PublicAPI]
public static class RunActivityExtensions
{
    /// <summary>
    /// Runs the specified activity.
    /// </summary>
    /// <param name="services">The service provider.</param>
    /// <param name="activity">The activity to run.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    /// <returns>The result of running the activity.</returns>
    public static async Task<RunWorkflowResult> RunActivityAsync(this IServiceProvider services, IActivity activity, CancellationToken cancellationToken = default)
    {
        await services.PopulateRegistriesAsync();
        var workflowRunner = services.GetRequiredService<IWorkflowRunner>();
        var result = await workflowRunner.RunAsync(activity, cancellationToken: cancellationToken);
        return result;
    }

    /// <summary>
    /// Runs the specified activity.
    /// </summary>
    /// <param name="services">The service provider.</param>
    /// <param name="activity">The activity to run.</param>
    /// <param name="options">An set of options.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    /// <returns>The result of running the activity.</returns>
    public static async Task<RunWorkflowResult> RunActivityAsync(this IServiceProvider services, IActivity activity, RunWorkflowOptions options, CancellationToken cancellationToken = default)
    {
        await services.PopulateRegistriesAsync();
        var workflowRunner = services.GetRequiredService<IWorkflowRunner>();
        var result = await workflowRunner.RunAsync(activity, options, cancellationToken);
        return result;
    }
    
    public static async Task<RunWorkflowResult> RunWorkflowAsync(this IServiceProvider services, Workflow workflow, RunWorkflowOptions options, CancellationToken cancellationToken = default)
    {
        return await services.RunActivityAsync(workflow, options, cancellationToken);
    }
}