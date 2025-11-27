using Elsa.Workflows.Activities;
using Elsa.Workflows.Builders;
using Elsa.Workflows.Memory;

namespace Elsa.Workflows;

/// <summary>
/// Extension methods for fluent workflow building.
/// </summary>
public static class WorkflowBuilderFluentExtensions
{
    /// <summary>
    /// Starts the workflow with an activity of the specified type.
    /// </summary>
    /// <typeparam name="TActivity">The type of activity to start with.</typeparam>
    /// <param name="workflowBuilder">The workflow builder.</param>
    /// <param name="setup">An optional action to configure the activity.</param>
    /// <returns>An activity builder for chaining.</returns>
    public static IActivityBuilder StartWith<TActivity>(this IWorkflowBuilder workflowBuilder, Action<TActivity>? setup = null) where TActivity : IActivity
    {
        var activity = Activator.CreateInstance<TActivity>();
        setup?.Invoke(activity);
        return new ActivityBuilder(workflowBuilder, activity);
    }
    
    /// <summary>
    /// Starts the workflow with the specified activity instance.
    /// </summary>
    /// <param name="workflowBuilder">The workflow builder.</param>
    /// <param name="activity">The activity instance to start with.</param>
    /// <returns>An activity builder for chaining.</returns>
    public static IActivityBuilder StartWith(this IWorkflowBuilder workflowBuilder, IActivity activity)
    {
        return new ActivityBuilder(workflowBuilder, activity);
    }
    
    /// <summary>
    /// Creates a workflow builder with the specified name and version.
    /// </summary>
    /// <param name="name">The name of the workflow.</param>
    /// <param name="version">The version of the workflow.</param>
    /// <returns>A workflow builder.</returns>
    public static IWorkflowBuilder CreateWorkflow(string name, int version = 1)
    {
        // This will be called statically, so we need a factory method
        // For now, this is a placeholder that will be implemented properly
        throw new NotImplementedException("Use dependency injection to get IWorkflowBuilderFactory");
    }
    
    /// <summary>
    /// Adds a variable to the workflow with the specified name.
    /// </summary>
    /// <param name="workflowBuilder">The workflow builder.</param>
    /// <param name="name">The name of the variable.</param>
    /// <param name="defaultValue">The default value of the variable.</param>
    /// <returns>The workflow builder for chaining.</returns>
    public static IWorkflowBuilder WithVariable(this IWorkflowBuilder workflowBuilder, string name, object? defaultValue = null)
    {
        // Create a Variable<object> since we don't know the type at compile time
        var variable = new Variable<object>(name, defaultValue ?? new object());
        return workflowBuilder.WithVariable(variable);
    }
    
    /// <summary>
    /// Adds a workflow name to the workflow builder.
    /// </summary>
    /// <param name="workflowBuilder">The workflow builder.</param>
    /// <param name="name">The name of the workflow.</param>
    /// <returns>The workflow builder for chaining.</returns>
    public static IWorkflowBuilder WithName(this IWorkflowBuilder workflowBuilder, string name)
    {
        workflowBuilder.Name = name;
        return workflowBuilder;
    }
    
    /// <summary>
    /// Adds a workflow version to the workflow builder.
    /// </summary>
    /// <param name="workflowBuilder">The workflow builder.</param>
    /// <param name="version">The version of the workflow.</param>
    /// <returns>The workflow builder for chaining.</returns>
    public static IWorkflowBuilder WithVersion(this IWorkflowBuilder workflowBuilder, int version)
    {
        workflowBuilder.Version = version;
        return workflowBuilder;
    }
    
    /// <summary>
    /// Builds the workflow.
    /// </summary>
    /// <param name="workflowBuilder">The workflow builder.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The built workflow.</returns>
    public static Task<Workflow> BuildAsync(this IWorkflowBuilder workflowBuilder, CancellationToken cancellationToken = default)
    {
        return workflowBuilder.BuildWorkflowAsync(cancellationToken);
    }
}
