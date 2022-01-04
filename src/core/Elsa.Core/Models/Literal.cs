namespace Elsa.Models;

public class Literal : RegisterLocationReference
{
    public Literal()
    {
    }

    public Literal(object? value)
    {
        Value = value;
    }
        
    public object? Value { get; }
    public override RegisterLocation Declare() => new();

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

    public new T? Get(ActivityExecutionContext context) => (T?)base.Get(context);
}