using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Memory;

namespace Elsa.Workflows.Core.Abstractions;

/// <summary>
/// A base class for implementing workflow definitions using the pipelineBuilder API.
/// </summary>
public abstract class WorkflowBase : IWorkflow
{
    /// <summary>
    /// Invokes the the specified <see cref="IWorkflowBuilder"/>.
    /// </summary>
    protected virtual ValueTask BuildAsync(IWorkflowBuilder builder, CancellationToken cancellationToken = default)
    {
        Build(builder);
        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// Invokes the specified <see cref="IWorkflowBuilder"/>.
    /// </summary>
    /// <param name="builder"></param>
    protected virtual void Build(IWorkflowBuilder builder)
    {
    }

    /// <summary>
    /// Gives derived types a chance to setup the <see cref="IWorkflowBuilder"/> before the BuildAsync method is invoked.
    /// </summary>
    protected virtual ValueTask BeforeBuildAsync(IWorkflowBuilder builder, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
    
    /// <summary>
    /// Gives derived types a chance to setup the <see cref="IWorkflowBuilder"/> after the BuildAsync method was invoked.
    /// </summary>
    protected virtual ValueTask AfterBuildAsync(IWorkflowBuilder builder, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;

    async ValueTask IWorkflow.BuildAsync(IWorkflowBuilder builder, CancellationToken cancellationToken)
    {
        await BeforeBuildAsync(builder, cancellationToken);
        await BuildAsync(builder, cancellationToken);
        await AfterBuildAsync(builder, cancellationToken);
    }
}

/// <summary>
/// A base class for implementing workflow definitions that can return a result using the pipelineBuilder API.
/// </summary>
public abstract class WorkflowBase<TResult> : WorkflowBase
{
    /// <inheritdoc />
    protected WorkflowBase()
    {
        Result = new Variable<TResult>();
    }
    
    /// <summary>
    /// Use this variable from your workflow to assign a result value. 
    /// </summary>
    protected Variable<TResult> Result { get; }

    /// <inheritdoc />
    protected override ValueTask BeforeBuildAsync(IWorkflowBuilder builder, CancellationToken cancellationToken = default)
    {
        builder.Result = Result;
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    protected override ValueTask AfterBuildAsync(IWorkflowBuilder builder, CancellationToken cancellationToken = default)
    {
        var variables = builder.Variables;
        
        if(!variables.Contains(Result))
           variables.Add(Result);

        builder.Variables = variables;
        
        return ValueTask.CompletedTask;
    }
}