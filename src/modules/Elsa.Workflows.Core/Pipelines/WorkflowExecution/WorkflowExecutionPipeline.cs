using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Middleware.Workflows;

namespace Elsa.Workflows.Core.Pipelines.WorkflowExecution;

/// <inheritdoc />
public class WorkflowExecutionPipeline : IWorkflowExecutionPipeline
{
    private readonly IServiceProvider _serviceProvider;
    private WorkflowMiddlewareDelegate? _pipeline;

    /// <summary>
    /// Constructor.
    /// </summary>
    public WorkflowExecutionPipeline(IServiceProvider serviceProvider, Action<IWorkflowExecutionPipelineBuilder> pipelineBuilder)
    {
        _serviceProvider = serviceProvider;
        Setup(pipelineBuilder);
    }

    /// <inheritdoc />
    public WorkflowMiddlewareDelegate Pipeline => _pipeline ??= CreateDefaultPipeline();

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

    private WorkflowMiddlewareDelegate CreateDefaultPipeline() => Setup(x => x.UseDefaultActivityScheduler());
}