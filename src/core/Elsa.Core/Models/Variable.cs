using Elsa.Helpers;

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
        Id = name;
        Name = name;
    }

    public string? Name { get; set; }
    public object? DefaultValue { get; set; }
    public override RegisterLocation Declare() => new(DefaultValue);
}

public class Variable<T> : Variable
{
    public Variable() : base(typeof(T).Name, default(T))
    {
    }

    public Variable(T value) : base(typeof(T).Name, value ?? default)
    {
    }

    public Variable(string name, T value = default!) : base(name, value ?? default)
    {
    }

    public new T? Get(ActivityExecutionContext context) => base.Get(context).ConvertTo<T?>();
    public new T? Get(ExpressionExecutionContext context) => base.Get(context).ConvertTo<T?>();
}