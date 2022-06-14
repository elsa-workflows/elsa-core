namespace Elsa.Expressions.Models;

public class DelegateBlockReference : MemoryBlockReference
{
    public DelegateBlockReference()
    {
    }

    public DelegateBlockReference(Func<object?> @delegate) => Delegate = _ => ValueTask.FromResult(@delegate());
    public DelegateBlockReference(Func<ExpressionExecutionContext, object?> @delegate) => Delegate = x => ValueTask.FromResult(@delegate(x));
    public DelegateBlockReference(Func<ExpressionExecutionContext, ValueTask<object?>> @delegate) => Delegate = @delegate;

    public Func<ExpressionExecutionContext, ValueTask<object?>>? Delegate { get; set; }
    public override MemoryBlock Declare() => new();
}

public class DelegateBlockReference<T> : DelegateBlockReference
{
    public DelegateBlockReference()
    {
    }

    public DelegateBlockReference(Func<T?> @delegate) : base(x => @delegate())
    {
    }
        
    public DelegateBlockReference(Func<ExpressionExecutionContext, T?> @delegate) : base(x => @delegate(x))
    {
    }
    
    public DelegateBlockReference(Func<ValueTask<T?>> @delegate) : base(_ => @delegate())
    {
    }
        
    public DelegateBlockReference(Func<ExpressionExecutionContext, ValueTask<T?>> @delegate) : base(x => @delegate(x))
    {
    }
}