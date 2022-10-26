using System;
using System.Threading.Tasks;
using Elsa.Workflows.Core.Middleware.Workflows;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Services;

namespace Elsa.Workflows.Core.Pipelines.WorkflowExecution;

public class WorkflowExecutionPipeline : IWorkflowExecutionPipeline
{
    private readonly IServiceProvider _serviceProvider;
    private WorkflowMiddlewareDelegate? _pipeline;
        
    public WorkflowExecutionPipeline(IServiceProvider serviceProvider) => _serviceProvider = serviceProvider;
    public WorkflowMiddlewareDelegate Pipeline => _pipeline ??= CreateDefaultPipeline();

    public WorkflowMiddlewareDelegate Setup(Action<IWorkflowExecutionBuilder> setup)
    {
        var builder = new WorkflowExecutionPipelineBuilder(_serviceProvider);
        setup(builder);
        _pipeline = builder.Build();
        return _pipeline;
    }
        
    public async Task ExecuteAsync(WorkflowExecutionContext context) => await Pipeline(context);

    private WorkflowMiddlewareDelegate CreateDefaultPipeline() => Setup(x => x
        .UseDefaultActivityScheduler()
    );
}