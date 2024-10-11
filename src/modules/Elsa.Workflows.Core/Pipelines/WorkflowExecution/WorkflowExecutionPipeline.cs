using Elsa.Workflows.Middleware.Workflows;

namespace Elsa.Workflows.Pipelines.WorkflowExecution;

/// <inheritdoc />
public class WorkflowExecutionPipeline : IWorkflowExecutionPipeline
{
    private readonly IServiceProvider _serviceProvider;
    private WorkflowMiddlewareDelegate? _pipeline;

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
        _pipeline = builder.Build();
        return _pipeline;
    }

    /// <inheritdoc />
    public async Task ExecuteAsync(WorkflowExecutionContext context) => await Pipeline(context);

    private WorkflowMiddlewareDelegate CreateDefaultPipeline() => Setup(x => x
        .UseExceptionHandling()
        .UseDefaultActivityScheduler());
}