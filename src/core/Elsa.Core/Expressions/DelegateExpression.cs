using Elsa.Contracts;
using Elsa.Models;

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