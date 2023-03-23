using Elsa.Workflows.Core.Contracts;

namespace Elsa.Workflows.Core.Pipelines.WorkflowExecution;

/// <inheritdoc />
public class WorkflowExecutionPipelineBuilder : IWorkflowExecutionPipelineBuilder
{
    private const string ServicesKey = "workflow-execution.Services";
    private readonly IList<Func<WorkflowMiddlewareDelegate, WorkflowMiddlewareDelegate>> _components = new List<Func<WorkflowMiddlewareDelegate, WorkflowMiddlewareDelegate>>();

    /// <summary>
    /// Constructor.
    /// </summary>
    public WorkflowExecutionPipelineBuilder(IServiceProvider serviceProvider)
    {
        ServiceProvider = serviceProvider;
    }

    /// <inheritdoc />
    public IDictionary<object, object?> Properties { get; } = new Dictionary<object, object?>();

    /// <inheritdoc />
    public IServiceProvider ServiceProvider
    {
        get => GetProperty<IServiceProvider>(ServicesKey)!;
        set => SetProperty(ServicesKey, value);
    }

    /// <inheritdoc />
    public IWorkflowExecutionPipelineBuilder Use(Func<WorkflowMiddlewareDelegate, WorkflowMiddlewareDelegate> middleware)
    {
        _components.Add(middleware);
        return this;
    }

    /// <inheritdoc />
    public WorkflowMiddlewareDelegate Build()
    {
        WorkflowMiddlewareDelegate pipeline = _ => new ValueTask();

        foreach (var component in _components.Reverse()) 
            pipeline = component(pipeline);

        return pipeline;
    }

    /// <inheritdoc />
    public IWorkflowExecutionPipelineBuilder Reset()
    {
        _components.Clear();
        return this;
    }

    private T? GetProperty<T>(string key) => Properties.TryGetValue(key, out var value) ? (T?)value : default(T);
    private void SetProperty<T>(string key, T value) => Properties[key] = value;
}