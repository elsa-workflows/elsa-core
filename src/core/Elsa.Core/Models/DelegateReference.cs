namespace Elsa.Models;

public class DelegateReference : RegisterLocationReference
{
    public DelegateReference()
    {
    }

    public DelegateReference(Func<object?> @delegate) => Delegate = _ => ValueTask.FromResult(@delegate());
    public DelegateReference(Func<ExpressionExecutionContext, object?> @delegate) => Delegate = x => ValueTask.FromResult(@delegate(x));
    public DelegateReference(Func<ExpressionExecutionContext, ValueTask<object?>> @delegate) => Delegate = @delegate;

    public Func<ExpressionExecutionContext, ValueTask<object?>>? Delegate { get; set; }
    public override RegisterLocation Declare() => new();
}

public class DelegateReference<T> : DelegateReference
{
    public DelegateReference()
    {
    }

    public DelegateReference(Func<T?> @delegate) : base(x => @delegate())
    {
    }
        
    public DelegateReference(Func<ExpressionExecutionContext, T?> @delegate) : base(x => @delegate(x))
    {
    }
        
    public DelegateReference(Func<ExpressionExecutionContext, ValueTask<T?>> @delegate) : base(x => @delegate(x))
    {
    }
}