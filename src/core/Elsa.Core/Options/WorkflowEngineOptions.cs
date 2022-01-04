using System.Collections.ObjectModel;

namespace Elsa.Options;

public class WorkflowEngineOptions
{
    private readonly IDictionary<Type, Type> _expressionHandlers = new Dictionary<Type, Type>();

    public WorkflowEngineOptions()
    {
        ExpressionHandlers = new ReadOnlyDictionary<Type, Type>(_expressionHandlers);
    }
        
    public IDictionary<Type, Type> ExpressionHandlers { get; }

    public WorkflowEngineOptions RegisterExpressionHandler(Type expression, Type handler)
    {
        _expressionHandlers.Add(expression, handler);
        return this;
    }

    public WorkflowEngineOptions RegisterExpressionHandler<THandler, TExpression>() => RegisterExpressionHandler<THandler>(typeof(TExpression));
        
    public WorkflowEngineOptions RegisterExpressionHandler<THandler>(Type expression)
    {
        _expressionHandlers.Add(expression, typeof(THandler));
        return this;
    }
}