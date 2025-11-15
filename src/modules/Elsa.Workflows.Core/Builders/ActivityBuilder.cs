using Elsa.Workflows.Activities;

namespace Elsa.Workflows.Builders;

/// <summary>
/// A fluent builder for chaining activities in a workflow.
/// </summary>
public class ActivityBuilder : IActivityBuilder
{
    private readonly List<IActivity> _activities = new();
    
    /// <summary>
    /// Initializes a new instance of the <see cref="ActivityBuilder"/> class.
    /// </summary>
    /// <param name="workflowBuilder">The workflow builder.</param>
    /// <param name="activity">The current activity.</param>
    public ActivityBuilder(IWorkflowBuilder workflowBuilder, IActivity activity)
    {
        WorkflowBuilder = workflowBuilder;
        Activity = activity;
        _activities.Add(activity);
    }
    
    private ActivityBuilder(IWorkflowBuilder workflowBuilder, IActivity activity, List<IActivity> previousActivities)
    {
        WorkflowBuilder = workflowBuilder;
        Activity = activity;
        _activities = new List<IActivity>(previousActivities) { activity };
    }
    
    /// <inheritdoc />
    public IWorkflowBuilder WorkflowBuilder { get; }
    
    /// <inheritdoc />
    public IActivity Activity { get; }
    
    /// <inheritdoc />
    public IActivityBuilder Then<TActivity>(Action<TActivity>? setup = null) where TActivity : IActivity
    {
        var activity = Activator.CreateInstance<TActivity>();
        setup?.Invoke(activity);
        return Then(activity);
    }
    
    /// <inheritdoc />
    public IActivityBuilder Then(IActivity activity)
    {
        return new ActivityBuilder(WorkflowBuilder, activity, _activities);
    }
    
    /// <inheritdoc />
    public Task<Workflow> BuildAsync(CancellationToken cancellationToken = default)
    {
        // Build the activity chain into a Sequence
        var sequence = new Sequence();
        foreach (var activity in _activities)
        {
            sequence.Activities.Add(activity);
        }
        
        WorkflowBuilder.Root = sequence;
        return WorkflowBuilder.BuildWorkflowAsync(cancellationToken);
    }
}
