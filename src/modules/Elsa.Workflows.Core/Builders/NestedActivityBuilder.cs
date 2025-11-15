using Elsa.Workflows.Activities;

namespace Elsa.Workflows.Builders;

/// <summary>
/// A builder for nested activities (e.g., inside If, While branches).
/// </summary>
internal class NestedActivityBuilder : IActivityBuilder
{
    private readonly List<IActivity> _activities = new();
    
    /// <summary>
    /// Initializes a new instance of the <see cref="NestedActivityBuilder"/> class.
    /// </summary>
    /// <param name="workflowBuilder">The workflow builder.</param>
    public NestedActivityBuilder(IWorkflowBuilder workflowBuilder)
    {
        WorkflowBuilder = workflowBuilder;
        // Create a placeholder activity for the Activity property
        Activity = new Sequence();
    }
    
    /// <inheritdoc />
    public IWorkflowBuilder WorkflowBuilder { get; }
    
    /// <inheritdoc />
    public IActivity Activity { get; private set; }
    
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
        _activities.Add(activity);
        Activity = activity;
        return this;
    }
    
    /// <inheritdoc />
    public Task<Workflow> BuildAsync(CancellationToken cancellationToken = default)
    {
        // Build the activity chain
        WorkflowBuilder.Root = BuildActivity();
        return WorkflowBuilder.BuildWorkflowAsync(cancellationToken);
    }
    
    /// <summary>
    /// Builds the activity chain into a single activity or sequence.
    /// </summary>
    /// <returns>The built activity.</returns>
    public IActivity BuildActivity()
    {
        if (_activities.Count == 0)
        {
            return new Sequence(); // Empty sequence
        }
        
        if (_activities.Count == 1)
        {
            return _activities[0];
        }
        
        var sequence = new Sequence();
        foreach (var activity in _activities)
        {
            sequence.Activities.Add(activity);
        }
        return sequence;
    }
}
