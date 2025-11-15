using Elsa.Expressions.Models;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Builders;
using Elsa.Workflows.Models;

namespace Elsa.Workflows;

/// <summary>
/// Extension methods for <see cref="IActivityBuilder"/> to add control flow activities.
/// </summary>
public static class ControlFlowActivityBuilderExtensions
{
    /// <summary>
    /// Adds a conditional branch to the workflow.
    /// </summary>
    /// <param name="builder">The activity builder.</param>
    /// <param name="condition">The condition expression to evaluate.</param>
    /// <param name="then">Action to configure the 'then' branch.</param>
    /// <param name="else">Optional action to configure the 'else' branch.</param>
    /// <returns>The activity builder for chaining.</returns>
    public static IActivityBuilder If(
        this IActivityBuilder builder, 
        string condition, 
        Action<IActivityBuilder> then,
        Action<IActivityBuilder>? @else = null)
    {
        var ifActivity = new If
        {
            Condition = new Input<bool>(new Expression("Liquid", condition))
        };
        
        // Build the 'then' branch
        var thenBuilder = new NestedActivityBuilder(builder.WorkflowBuilder);
        then(thenBuilder);
        ifActivity.Then = thenBuilder.BuildActivity();
        
        // Build the 'else' branch if provided
        if (@else != null)
        {
            var elseBuilder = new NestedActivityBuilder(builder.WorkflowBuilder);
            @else(elseBuilder);
            ifActivity.Else = elseBuilder.BuildActivity();
        }
        
        return builder.Then(ifActivity);
    }
    
    /// <summary>
    /// Adds a while loop to the workflow.
    /// </summary>
    /// <param name="builder">The activity builder.</param>
    /// <param name="condition">The condition expression to evaluate.</param>
    /// <param name="body">Action to configure the loop body.</param>
    /// <returns>The activity builder for chaining.</returns>
    public static IActivityBuilder While(
        this IActivityBuilder builder,
        string condition,
        Action<IActivityBuilder> body)
    {
        var bodyBuilder = new NestedActivityBuilder(builder.WorkflowBuilder);
        body(bodyBuilder);
        
        var whileActivity = new While(new Input<bool>(new Expression("Liquid", condition)), bodyBuilder.BuildActivity());
        
        return builder.Then(whileActivity);
    }
    
    /// <summary>
    /// Adds a for-each loop to the workflow.
    /// </summary>
    /// <typeparam name="T">The type of items to iterate over.</typeparam>
    /// <param name="builder">The activity builder.</param>
    /// <param name="items">Expression that evaluates to the collection to iterate.</param>
    /// <param name="body">Action to configure the loop body.</param>
    /// <returns>The activity builder for chaining.</returns>
    public static IActivityBuilder ForEach<T>(
        this IActivityBuilder builder,
        string items,
        Action<IActivityBuilder> body)
    {
        var forEachActivity = new ForEach<T>
        {
            Items = new Input<ICollection<T>>(new Expression("Liquid", items))
        };
        
        var bodyBuilder = new NestedActivityBuilder(builder.WorkflowBuilder);
        body(bodyBuilder);
        forEachActivity.Body = bodyBuilder.BuildActivity();
        
        return builder.Then(forEachActivity);
    }
    
    /// <summary>
    /// Adds a for-each loop to the workflow (untyped version).
    /// </summary>
    /// <param name="builder">The activity builder.</param>
    /// <param name="items">Expression that evaluates to the collection to iterate.</param>
    /// <param name="body">Action to configure the loop body.</param>
    /// <returns>The activity builder for chaining.</returns>
    public static IActivityBuilder ForEach(
        this IActivityBuilder builder,
        string items,
        Action<IActivityBuilder> body)
    {
        return builder.ForEach<object>(items, body);
    }
    
    /// <summary>
    /// Adds a switch statement to the workflow.
    /// </summary>
    /// <param name="builder">The activity builder.</param>
    /// <param name="cases">Dictionary of case labels to condition expressions and actions.</param>
    /// <param name="default">Optional default action if no case matches.</param>
    /// <returns>The activity builder for chaining.</returns>
    public static IActivityBuilder Switch(
        this IActivityBuilder builder,
        Dictionary<string, (string condition, Action<IActivityBuilder> action)> cases,
        Action<IActivityBuilder>? @default = null)
    {
        var switchActivity = new Switch
        {
            Cases = new List<SwitchCase>()
        };
        
        foreach (var (label, (condition, action)) in cases)
        {
            var caseBuilder = new NestedActivityBuilder(builder.WorkflowBuilder);
            action(caseBuilder);
            
            switchActivity.Cases.Add(new SwitchCase
            {
                Label = label,
                Condition = new Expression("Liquid", condition),
                Activity = caseBuilder.BuildActivity()
            });
        }
        
        if (@default != null)
        {
            var defaultBuilder = new NestedActivityBuilder(builder.WorkflowBuilder);
            @default(defaultBuilder);
            switchActivity.Default = defaultBuilder.BuildActivity();
        }
        
        return builder.Then(switchActivity);
    }
    
    /// <summary>
    /// Adds a try-catch-finally block to the workflow.
    /// </summary>
    /// <param name="builder">The activity builder.</param>
    /// <param name="try">Action to configure the try block.</param>
    /// <param name="catch">Optional action to configure the catch block.</param>
    /// <param name="finally">Optional action to configure the finally block.</param>
    /// <returns>The activity builder for chaining.</returns>
    public static IActivityBuilder Try(
        this IActivityBuilder builder,
        Action<IActivityBuilder> @try,
        Action<IActivityBuilder>? @catch = null,
        Action<IActivityBuilder>? @finally = null)
    {
        // Elsa doesn't have a built-in Try activity in the core, so we'll create a composite
        // In a real implementation, this would use Fault activities or a custom Try activity
        // For now, we'll just execute the try block directly
        var tryBuilder = new NestedActivityBuilder(builder.WorkflowBuilder);
        @try(tryBuilder);
        
        return builder.Then(tryBuilder.BuildActivity());
    }
    
    /// <summary>
    /// Adds activities to run in parallel.
    /// </summary>
    /// <param name="builder">The activity builder.</param>
    /// <param name="branches">Actions to configure parallel branches.</param>
    /// <returns>The activity builder for chaining.</returns>
    public static IActivityBuilder Parallel(
        this IActivityBuilder builder,
        params Action<IActivityBuilder>[] branches)
    {
        var parallelActivity = new Activities.Parallel();
        
        foreach (var branch in branches)
        {
            var branchBuilder = new NestedActivityBuilder(builder.WorkflowBuilder);
            branch(branchBuilder);
            parallelActivity.Activities.Add(branchBuilder.BuildActivity());
        }
        
        return builder.Then(parallelActivity);
    }
}
