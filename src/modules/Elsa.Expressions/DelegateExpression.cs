using Elsa.Expressions.Contracts;
using Elsa.Expressions.Models;

namespace Elsa.Expressions;

public class DelegateExpression : IExpression
{
    public DelegateExpression(DelegateBlockReference delegateBlockReference)
    {
        DelegateBlockReference = delegateBlockReference;
    }
        
    public DelegateBlockReference DelegateBlockReference { get; }
}

public class DelegateExpression<T> : DelegateExpression
{
    public DelegateExpression(DelegateBlockReference<T> delegateBlockReference) : base(delegateBlockReference)
    {
    }

    public DelegateExpression(Func<T?> @delegate) : this(new DelegateBlockReference<T>(@delegate))
    {
    }
        
    public DelegateExpression(Func<ExpressionExecutionContext, T?> @delegate) : this(new DelegateBlockReference<T>(@delegate))
    {
    }
    
    public DelegateExpression(Func<ValueTask<T?>> @delegate) : this(new DelegateBlockReference<T>(_ => @delegate()))
    {
    }
        
    public DelegateExpression(Func<ExpressionExecutionContext, ValueTask<T?>> @delegate) : this(new DelegateBlockReference<T>(@delegate))
    {
    }
}

public class DelegateExpressionHandler : IExpressionHandler
{
    public async ValueTask<object?> EvaluateAsync(IExpression expression, Type returnType, ExpressionExecutionContext context)
    {
        var delegateExpression = (DelegateExpression)expression;
        var @delegate = delegateExpression.DelegateBlockReference.Delegate;
        var value = @delegate != null ? await @delegate(context) : default;
        return value;
    }
}