using Elsa.Contracts;
using Elsa.Expressions;

namespace Elsa.Models;

public abstract class Input : Argument
{
    protected Input(IExpression expression, RegisterLocationReference locationReference, Type type) : base(locationReference)
    {
        Expression = expression;
        Type = type;
    }

    public IExpression Expression { get; }
    public Type Type { get; set; }
}

public class Input<T> : Input
{
    public Input(T literal) : this(new Literal<T>(literal))
    {
    }

    public Input(Func<T> @delegate) : this(new DelegateReference(() => @delegate()))
    {
    }

    public Input(Func<ExpressionExecutionContext, T> @delegate) : this(new DelegateReference<T>(@delegate))
    {
    }

    public Input(Variable<T> variable) : base(new VariableExpression(variable), variable, typeof(T))
    {
    }

    public Input(Literal<T> literal) : base(new LiteralExpression(literal.Value), literal, typeof(T))
    {
    }
        
    public Input(Literal literal) : base(new LiteralExpression(literal.Value), literal, typeof(T))
    {
    }

    public Input(DelegateReference delegateReference) : base(new DelegateExpression(delegateReference), delegateReference, typeof(T))
    {
    }

    public Input(ElsaExpression expression) : this(new ElsaExpressionReference(expression))
    {
    }

    public Input(IExpression expression, RegisterLocationReference locationReference) : base(expression, locationReference, typeof(T))
    {
    }

    private Input(ElsaExpressionReference expressionReference) : base(expressionReference.Expression, expressionReference, typeof(T))
    {
    }
}