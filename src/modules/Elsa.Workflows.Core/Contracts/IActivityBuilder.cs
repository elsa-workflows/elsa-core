using Elsa.Workflows.Activities;

namespace Elsa.Workflows;

/// <summary>
/// A fluent builder for chaining activities in a workflow.
/// </summary>
public interface IActivityBuilder
{
    /// <summary>
    /// Gets the workflow builder associated with this activity builder.
    /// </summary>
    IWorkflowBuilder WorkflowBuilder { get; }
    
    /// <summary>
    /// Gets the current activity being built.
    /// </summary>
    IActivity Activity { get; }
    
    /// <summary>
    /// Adds an activity to execute after the current activity.
    /// </summary>
    /// <typeparam name="TActivity">The type of activity to add.</typeparam>
    /// <param name="setup">An optional action to configure the activity.</param>
    /// <returns>A new activity builder for chaining.</returns>
    IActivityBuilder Then<TActivity>(Action<TActivity>? setup = null) where TActivity : IActivity;
    
    /// <summary>
    /// Adds an activity instance to execute after the current activity.
    /// </summary>
    /// <param name="activity">The activity instance to add.</param>
    /// <returns>A new activity builder for chaining.</returns>
    IActivityBuilder Then(IActivity activity);
    
    /// <summary>
    /// Builds the workflow and returns the workflow instance.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The built workflow.</returns>
    Task<Workflow> BuildAsync(CancellationToken cancellationToken = default);
}
