using Elsa.Expressions.Models;
using Elsa.Extensions;

namespace Elsa.CSharp.Models;

/// <summary>
/// Provides access to global objects, such as the workflow execution context.
/// </summary>
public partial class Globals
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Globals"/> class.
    /// </summary>
    public Globals(ExpressionExecutionContext expressionExecutionContext)
    {
        ExpressionExecutionContext = expressionExecutionContext;
        ExecutionContext = new ExecutionContextProxy(expressionExecutionContext);
        Input = new InputProxy(expressionExecutionContext);
        Output = new OutputProxy(expressionExecutionContext);
        Outcome = new OutcomeProxy(expressionExecutionContext);
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
    /// Provides access to workflow inputs.
    /// </summary>
    public InputProxy Input { get; set; }

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
    
    private ExpressionExecutionContext ExpressionExecutionContext { get; }
}