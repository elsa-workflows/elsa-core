using Elsa.Workflows.Middleware.Workflows;

namespace Elsa.Workflows.Pipelines.WorkflowExecution;

/// <inheritdoc />
public class WorkflowExecutionPipeline : IWorkflowExecutionPipeline
{
    private readonly IServiceProvider _serviceProvider;
    private WorkflowMiddlewareDelegate? _pipeline;
    private WorkflowMiddlewareDelegate? _runnerPipeline;
    private readonly object _gate = new();
    private long _generation;

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
        var raw = builder.BuildInternal();
        lock (_gate)
        {
            _runnerPipeline = raw;
            _pipeline = WorkflowExecutionPipelineBuilder.Guard(raw, _serviceProvider);
            _generation++;
            return _pipeline;
        }
    }

    /// <inheritdoc />
    public async Task ExecuteAsync(WorkflowExecutionContext context) => await Pipeline(context);

    internal (Action Validate, WorkflowMiddlewareDelegate Execute) CaptureAuthorizedInvocation()
    {
        lock (_gate)
        {
            var pipeline = _runnerPipeline ?? throw new InvalidOperationException("The workflow pipeline is not configured.");
            var generation = _generation;
            void Validate()
            {
                lock (_gate)
                {
                    if (generation != _generation || !ReferenceEquals(pipeline, _runnerPipeline))
                    {
                        throw new WorkflowPipelineChangedException();
                    }
                }
            }
            return (Validate, context =>
            {
                lock (_gate)
                {
                    Validate();
                    return pipeline(context);
                }
            });
        }
    }

    private WorkflowMiddlewareDelegate CreateDefaultPipeline() => Setup(x => x
        .UseExceptionHandling()
        .UseDefaultActivityScheduler());
}
internal sealed class WorkflowPipelineChangedException() : InvalidOperationException("The authorized workflow composition changed.");
