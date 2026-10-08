using Elsa.Workflows.Middleware.Workflows;

namespace Elsa.Workflows.Pipelines.WorkflowExecution;

/// <inheritdoc />
public class WorkflowExecutionPipeline : IWorkflowExecutionPipeline
{
    private readonly IServiceProvider _serviceProvider;
    private WorkflowMiddlewareDelegate? _pipeline;
    private WorkflowMiddlewareDelegate? _runnerPipeline;

    /// <summary>
    /// Initializes a new instance of the <see cref="WorkflowExecutionPipeline"/> class.
    /// </summary>
    public WorkflowExecutionPipeline(IServiceProvider serviceProvider, Action<IWorkflowExecutionPipelineBuilder> configurePipelineBuilder)
    {
        _serviceProvider = serviceProvider;
        ConfigurePipelineBuilder = configurePipelineBuilder;
        Setup(configurePipelineBuilder);
    }
    
    /// <inheritdoc />
    public WorkflowMiddlewareDelegate Pipeline => _pipeline ??= CreateDefaultPipeline();

    /// <inheritdoc />
    public Action<IWorkflowExecutionPipelineBuilder> ConfigurePipelineBuilder { get; }

    /// <inheritdoc />
    public WorkflowMiddlewareDelegate Setup(Action<IWorkflowExecutionPipelineBuilder> setup)
    {
        var builder = new WorkflowExecutionPipelineBuilder(_serviceProvider);
        setup(builder);
        _runnerPipeline = builder.BuildInternal();
        _pipeline = WorkflowExecutionPipelineBuilder.Guard(_runnerPipeline);
        return _pipeline;
    }

    /// <inheritdoc />
    public async Task ExecuteAsync(WorkflowExecutionContext context) => await Pipeline(context);

    internal async Task ExecuteAuthorizedAsync(WorkflowExecutionContext context, IWorkflowExecutionAuthorization authorization)
    {
        await authorization.RevalidateAsync(context.CancellationToken);
        await (_runnerPipeline ?? throw new InvalidOperationException("The workflow pipeline is not configured."))(context);
    }

    private WorkflowMiddlewareDelegate CreateDefaultPipeline() => Setup(x => x
        .UseExceptionHandling()
        .UseDefaultActivityScheduler());
}