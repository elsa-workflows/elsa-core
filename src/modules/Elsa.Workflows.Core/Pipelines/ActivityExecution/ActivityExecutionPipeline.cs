using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Middleware.Activities;
using Elsa.Workflows.Core.Models;

namespace Elsa.Workflows.Core.Pipelines.ActivityExecution;

/// <inheritdoc />
public class ActivityExecutionPipeline : IActivityExecutionPipeline
{
    private readonly IServiceProvider _serviceProvider;
    private ActivityMiddlewareDelegate? _pipeline;

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
        var builder = new ActivityExecutionPipelinePipelineBuilder(_serviceProvider);
        setup(builder);
        _pipeline = builder.Build();
        return _pipeline;
    }

    /// <inheritdoc />
    public ActivityMiddlewareDelegate Pipeline => _pipeline ??= CreateDefaultPipeline();

    /// <inheritdoc />
    public async Task ExecuteAsync(ActivityExecutionContext context) => await Pipeline(context);
        
    private ActivityMiddlewareDelegate CreateDefaultPipeline() => Setup(x => x
        .UseExceptionHandling()
        .UseExecutionLogging()
        .UseDefaultActivityInvoker()
    );
}