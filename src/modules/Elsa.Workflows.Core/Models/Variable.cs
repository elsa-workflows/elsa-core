using Elsa.Expressions.Helpers;
using Elsa.Expressions.Models;

namespace Elsa.Workflows.Core.Models;

public class Variable : MemoryDatumReference
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
    public override MemoryDatum Declare() => new(DefaultValue);
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

    public T? Get(ActivityExecutionContext context) => Get(context.ExpressionExecutionContext).ConvertTo<T?>();
    public new T? Get(ExpressionExecutionContext context) => base.Get(context).ConvertTo<T?>();
}