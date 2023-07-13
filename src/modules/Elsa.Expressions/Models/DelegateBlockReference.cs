namespace Elsa.Expressions.Models;

public class DelegateBlockReference : MemoryBlockReference
{
    public DelegateBlockReference(string? id = default) : base(id)
    {
    }

    public DelegateBlockReference(Func<object?> @delegate, string? id = default) : this(id) => Delegate = _ => ValueTask.FromResult(@delegate());
    public DelegateBlockReference(Func<ExpressionExecutionContext, object?> @delegate, string? id = default) : this(id) => Delegate = x => ValueTask.FromResult(@delegate(x));
    public DelegateBlockReference(Func<ExpressionExecutionContext, ValueTask<object?>> @delegate, string? id = default) : this(id) => Delegate = @delegate;

    public Func<ExpressionExecutionContext, ValueTask<object?>>? Delegate { get; set; }
    public override MemoryBlock Declare() => new();
}

public class DelegateBlockReference<T> : DelegateBlockReference
{
    public DelegateBlockReference(string? id = default) : base(id)
    {
    }

    public DelegateBlockReference(Func<T?> @delegate, string? id = default) : base(x => @delegate(), id)
    {
    }

    public DelegateBlockReference(Func<ExpressionExecutionContext, T?> @delegate, string? id = default) : base(x => @delegate(x), id)
    {
    }

    public DelegateBlockReference(Func<ValueTask<T?>> @delegate, string? id = default) : base(_ => @delegate(), id)
    {
    }

    public DelegateBlockReference(Func<ExpressionExecutionContext, ValueTask<T?>> @delegate, string? id = default) : base(async x => await @delegate(x), id)
    {
    }
}