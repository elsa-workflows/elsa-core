using Elsa.Expressions.Models;
using Elsa.Extensions;
using JetBrains.Annotations;

namespace Elsa.Scripting.CSharp.Models;

/// <summary>
/// Provides access to global objects, such as the workflow execution context.
/// </summary>
[UsedImplicitly]
public partial class Globals
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Globals"/> class.
    /// </summary>
    public Globals(ExpressionExecutionContext expressionExecutionContext, IDictionary<string, object> arguments)
    {
        ExpressionExecutionContext = expressionExecutionContext;
        Arguments = arguments;
        ExecutionContext = new(expressionExecutionContext);
        Output = new(expressionExecutionContext);
        Outcome = new(expressionExecutionContext);
    }

    /// <summary>
    /// Provides access to activity outcomes.
    /// </summary>
    public OutcomeProxy Outcome { get; set; }

    /// <summary>
    /// Provides access to activity outputs.
    /// </summary>
    public OutputProxy Output { get; set; }

    /// <summary>
    /// Gets the current execution context.
    /// </summary>
    public ExecutionContextProxy ExecutionContext { get; }
    
    /// <summary>
    /// Gets the ID of the current workflow instance.
    /// </summary>
    public string WorkflowInstanceId => ExpressionExecutionContext.GetWorkflowExecutionContext().Id;
    
    /// <summary>
    /// Gets or sets the correlation ID of the current workflow instance.
    /// </summary>
    public string? CorrelationId
    {
        get => ExpressionExecutionContext.GetWorkflowExecutionContext().CorrelationId;
        set => ExpressionExecutionContext.GetWorkflowExecutionContext().CorrelationId = value;
    }

    /// <summary>
    /// Gets or sets the name of the current workflow instance.
    /// </summary>
    public string? WorkflowInstanceName
    {
        get => ExpressionExecutionContext.GetWorkflowExecutionContext().Name;
        set => ExpressionExecutionContext.GetWorkflowExecutionContext().Name = value;
    }
    
    /// <summary>
    /// Gets additional arguments provided by the caller of the evaluator.
    /// </summary>
    public IDictionary<string, object> Arguments { get; }
    
    private ExpressionExecutionContext ExpressionExecutionContext { get; }
}