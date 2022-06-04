using Elsa.Expressions.Helpers;
using Elsa.Expressions.Models;

namespace Elsa.Workflows.Core.Models;

public class Variable : MemoryReference
{
    public Variable()
    {
        Id = Guid.NewGuid().ToString("N");
    }

    public Variable(string name)
    {
        Id = name;
        Name = name;
    }

    public Variable(string name, object? defaultValue = default) : this(name)
    {
        DefaultValue = defaultValue;
    }

    public string? Name { get; set; }
    public object? DefaultValue { get; set; }
    public override MemoryBlock Declare() => new(DefaultValue);
}

public class Variable<T> : Variable
{
    public Variable()
    {
    }

    public Variable(string name) : base(name)
    {
    }

    public Variable(string name, T value) : base(name, value ?? default)
    {
    }
    
    public Variable(T value)
    {
        DefaultValue = value;
    }

    public T? Get(ActivityExecutionContext context) => Get(context.ExpressionExecutionContext).ConvertTo<T?>();
    public new T? Get(ExpressionExecutionContext context) => base.Get(context).ConvertTo<T?>();
}