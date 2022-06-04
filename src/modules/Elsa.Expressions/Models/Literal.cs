namespace Elsa.Expressions.Models;

public class Literal : MemoryReference
{
    public Literal()
    {
    }

    public Literal(object? value)
    {
        Value = value;
    }
        
    public object? Value { get; }
    public override MemoryBlock Declare() => new();

    public static Literal From<T>(T value) => new Literal<T>(value);
}

public class Literal<T> : Literal
{
    public Literal()
    {
    }

    public Literal(T value) : base(value!)
    {
    }

    //public new T? Get(ActivityExecutionContext context) => (T?)base.Get(context);
}