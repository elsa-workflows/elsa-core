namespace Elsa.Models;

public class Variable : RegisterLocationReference
{
    public Variable()
    {
    }

    public Variable(object? defaultValue)
    {
        DefaultValue = defaultValue;
    }

    public Variable(string name, object? defaultValue = default) : this(defaultValue)
    {
        Name = name;
    }

    public string? Name { get; set; }
    public object? DefaultValue { get; }
    public override RegisterLocation Declare() => new(DefaultValue);
}

public class Variable<T> : Variable
{
    public Variable() : base(default(T))
    {
    }

    public Variable(T value) : base(value ?? default)
    {
    }
        
    public new T? Get(ExpressionExecutionContext context) => (T?)base.Get(context);
}