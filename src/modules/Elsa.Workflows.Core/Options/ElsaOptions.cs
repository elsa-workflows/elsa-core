using System.Collections.ObjectModel;

namespace Elsa.Workflows.Core.Options;

public class ElsaOptions
{
    private readonly IDictionary<Type, Type> _expressionHandlers = new Dictionary<Type, Type>();

    public ElsaOptions()
    {
        ExpressionHandlers = new ReadOnlyDictionary<Type, Type>(_expressionHandlers);
    }
        
    public IDictionary<Type, Type> ExpressionHandlers { get; }

    public ElsaOptions RegisterExpressionHandler(Type expression, Type handler)
    {
        _expressionHandlers.Add(expression, handler);
        return this;
    }

    public ElsaOptions RegisterExpressionHandler<THandler, TExpression>() => RegisterExpressionHandler<THandler>(typeof(TExpression));
        
    public ElsaOptions RegisterExpressionHandler<THandler>(Type expression)
    {
        _expressionHandlers.Add(expression, typeof(THandler));
        return this;
    }
}