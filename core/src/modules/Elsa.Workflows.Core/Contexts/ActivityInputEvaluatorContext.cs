using Elsa.Expressions.Contracts;
using Elsa.Expressions.Models;
using Elsa.Workflows.Models;

namespace Elsa.Workflows;

public record ActivityInputEvaluatorContext(
    ActivityExecutionContext ActivityExecutionContext, 
    ExpressionExecutionContext ExpressionExecutionContext, 
    InputDescriptor InputDescriptor, 
    Input Input, 
    IExpressionEvaluator ExpressionEvaluator);