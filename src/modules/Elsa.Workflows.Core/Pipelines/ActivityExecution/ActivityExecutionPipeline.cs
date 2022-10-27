using System;
using System.Threading.Tasks;
using Elsa.Workflows.Core.Middleware.Activities;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Services;

namespace Elsa.Workflows.Core.Pipelines.ActivityExecution;

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
        .UseDefaultActivityInvoker()
    );
}