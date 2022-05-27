using Elsa.Expressions.Models;
using Elsa.Expressions.Services;

namespace Elsa.Expressions;

public class DelegateExpression : IExpression
{
    public DelegateExpression(DelegateReference delegateReference)
    {
        DelegateReference = delegateReference;
    }
        
    public DelegateReference DelegateReference { get; }
}

public class DelegateExpression<T> : DelegateExpression
{
    public DelegateExpression(DelegateReference<T> delegateReference) : base(delegateReference)
    {
    }

    public DelegateExpression(Func<T?> @delegate) : this(new DelegateReference<T>(@delegate))
    {
    }
        
    public DelegateExpression(Func<ExpressionExecutionContext, T?> @delegate) : this(new DelegateReference<T>(@delegate))
    {
    }
    
    public DelegateExpression(Func<ValueTask<T?>> @delegate) : this(new DelegateReference<T>(_ => @delegate()))
    {
    }
        
    public DelegateExpression(Func<ExpressionExecutionContext, ValueTask<T?>> @delegate) : this(new DelegateReference<T>(@delegate))
    {
    }
}

public class DelegateExpressionHandler : IExpressionHandler
{
    public async ValueTask<object?> EvaluateAsync(IExpression expression, Type returnType, ExpressionExecutionContext context)
    {
        var delegateExpression = (DelegateExpression)expression;
        var @delegate = delegateExpression.DelegateReference.Delegate;
        var value = @delegate != null ? await @delegate(context) : default;
        return value;
    }
}