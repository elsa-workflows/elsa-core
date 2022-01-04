using Elsa.Contracts;
using Elsa.Models;
using Elsa.Pipelines.ActivityExecution.Components;

namespace Elsa.Pipelines.ActivityExecution;

public class ActivityExecutionPipeline : IActivityExecutionPipeline
{
    private readonly IServiceProvider _serviceProvider;
    private ActivityMiddlewareDelegate? _pipeline;

    public ActivityExecutionPipeline(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public ActivityMiddlewareDelegate Setup(Action<IActivityExecutionBuilder> setup)
    {
        var builder = new ActivityExecutionPipelineBuilder(_serviceProvider);
        setup(builder);
        _pipeline = builder.Build();
        return _pipeline;
    }

    public ActivityMiddlewareDelegate Pipeline => _pipeline ??= CreateDefaultPipeline();
    public async Task ExecuteAsync(ActivityExecutionContext context) => await Pipeline(context);
        
    private ActivityMiddlewareDelegate CreateDefaultPipeline() => Setup(x => x
        .UseActivityDrivers()
    );
}