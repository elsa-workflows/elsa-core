using Elsa.Workflows.Middleware.Activities;

namespace Elsa.Workflows.Pipelines.ActivityExecution;

/// <inheritdoc />
public class ActivityExecutionPipeline : IActivityExecutionPipeline
{
    private readonly IServiceProvider _serviceProvider;
    private ActivityMiddlewareDelegate? _pipeline;
    private ActivityMiddlewareDelegate? _invokerPipeline;
    private readonly object _gate = new();
    private bool _frozen;

    /// <summary>
    /// Constructor.
    /// </summary>
    public ActivityExecutionPipeline(IServiceProvider serviceProvider, Action<IActivityExecutionPipelineBuilder> pipelineBuilder)
    {
        _serviceProvider = serviceProvider;
        Setup(pipelineBuilder);
    }

    /// <inheritdoc />
    public ActivityMiddlewareDelegate Setup(Action<IActivityExecutionPipelineBuilder> setup)
    {
        lock (_gate)
        {
            if (_frozen)
            {
                throw new InvalidOperationException("This activity execution composition is frozen.");
            }
            var builder = new ActivityExecutionPipelinePipelineBuilder(_serviceProvider);
            setup(builder);
            _invokerPipeline = builder.BuildInternal();
            _pipeline = ActivityExecutionPipelinePipelineBuilder.Guard(_invokerPipeline, _serviceProvider);
            return _pipeline;
        }
    }

    /// <summary>
    /// Permanently prevents replacement before configuration callbacks or middleware constructors run.
    /// Ordinary hosts retain mutable compositions unless they explicitly opt in.
    /// </summary>
    public void Freeze()
    {
        lock (_gate)
        {
            _frozen = true;
        }
    }

    /// <inheritdoc />
    public ActivityMiddlewareDelegate Pipeline => _pipeline ??= CreateDefaultPipeline();

    /// <inheritdoc />
    public async Task ExecuteAsync(ActivityExecutionContext context) => await Pipeline(context);
        
    internal async ValueTask DemandPublicInvocationAsync(WorkflowExecutionContext context)
    {
        var guard = _serviceProvider.GetService(typeof(IWorkflowExecutionGuard)) as IWorkflowExecutionGuard ?? context.GetService<IWorkflowExecutionGuard>();
        if (guard != null)
        {
            await guard.AuthorizeAsync(context, WorkflowExecutionEntryPoint.DirectPipeline);
        }
    }

    internal ValueTask ExecuteAuthorizedAsync(ActivityExecutionContext context)
    {
        if (!WorkflowExecutionPhase.Contains(context.WorkflowExecutionContext))
        {
            throw new InvalidOperationException("The authorized activity invocation has unwound.");
        }
        return (_invokerPipeline ?? throw new InvalidOperationException("The activity pipeline is not configured."))(context);
    }

    private ActivityMiddlewareDelegate CreateDefaultPipeline() => Setup(x => x
        .UseLogging()
        .UseExceptionHandling()
        .UseExecutionLogging()
        .UseNotifications()
        .UseDefaultActivityInvoker()
    );
}