using Elsa.Workflows.Activities.Compensation;

namespace Elsa.Workflows.Pipelines.ActivityExecution;

/// <inheritdoc />
public class ActivityExecutionPipelinePipelineBuilder(IServiceProvider serviceProvider) : IActivityExecutionPipelineBuilder
{
    private readonly IList<Func<ActivityMiddlewareDelegate, ActivityMiddlewareDelegate>> _components = new List<Func<ActivityMiddlewareDelegate, ActivityMiddlewareDelegate>>();

    /// <inheritdoc />
    public IServiceProvider ServiceProvider { get; } = serviceProvider;

    /// <inheritdoc />
    public IActivityExecutionPipelineBuilder Use(Func<ActivityMiddlewareDelegate, ActivityMiddlewareDelegate> middleware)
    {
        _components.Add(middleware);
        return this;
    }

    /// <inheritdoc />
    public IActivityExecutionPipelineBuilder Insert(int index, Func<ActivityMiddlewareDelegate, ActivityMiddlewareDelegate> middleware)
    {
        _components.Insert(index, middleware);
        return this;
    }

    /// <inheritdoc />
    public ActivityMiddlewareDelegate Build() => Guard(BuildInternal(), ServiceProvider);

    internal static ActivityMiddlewareDelegate Guard(ActivityMiddlewareDelegate pipeline, IServiceProvider serviceProvider) => async context =>
    {
        var workflow = context.WorkflowExecutionContext;
        CompensationScopeExecutionGuard.DemandExecutable(workflow);
        var guard = serviceProvider.GetService(typeof(IWorkflowExecutionGuard)) as IWorkflowExecutionGuard ?? workflow.GetService<IWorkflowExecutionGuard>();
        if (guard != null)
        {
            await guard.AuthorizeAsync(workflow, WorkflowExecutionEntryPoint.DirectPipeline);
        }

        // Host authorization callbacks may mutate the workflow graph.
        CompensationScopeExecutionGuard.DemandExecutable(workflow);
        await pipeline(context);
    };

    internal ActivityMiddlewareDelegate BuildInternal()
    {
        ActivityMiddlewareDelegate pipeline = _ => new ValueTask();

        foreach (var component in _components.Reverse())
        {
            pipeline = component(pipeline);
        }

        return pipeline;
    }

    /// <inheritdoc />
    public IActivityExecutionPipelineBuilder Reset()
    {
        _components.Clear();
        return this;
    }
}
