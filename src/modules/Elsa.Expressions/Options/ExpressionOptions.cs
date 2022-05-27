using System.Collections.ObjectModel;

namespace Elsa.Expressions.Options;

public class ExpressionOptions
{
    private readonly IDictionary<Type, Type> _expressionHandlers = new Dictionary<Type, Type>();

    public ExpressionOptions()
    {
        ExpressionHandlers = new ReadOnlyDictionary<Type, Type>(_expressionHandlers);
    }
        
    public IDictionary<Type, Type> ExpressionHandlers { get; }

    public ExpressionOptions RegisterExpressionHandler(Type expression, Type handler)
    {
        _expressionHandlers.Add(expression, handler);
        return this;
    }

    public ExpressionOptions RegisterExpressionHandler<THandler, TExpression>() => RegisterExpressionHandler<THandler>(typeof(TExpression));
        
    public ExpressionOptions RegisterExpressionHandler<THandler>(Type expression)
    {
        _expressionHandlers.Add(expression, typeof(THandler));
        return this;
    }
}