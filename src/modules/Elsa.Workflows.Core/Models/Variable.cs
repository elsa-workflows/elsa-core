using Elsa.Expressions.Helpers;
using Elsa.Expressions.Models;

namespace Elsa.Workflows.Core.Models;

public class Variable : MemoryBlockReference
{
    public Variable()
    {
        Id = Guid.NewGuid().ToString("N");
    }

    public Variable(string name)
    {
        Id = name;
    }

    public Variable(string name, object? value = default) : this(name)
    {
        Value = value;
    }

    public string Name
    {
        get => Id;
        set => Id = value;
    }
    
    public object? Value { get; set; }
    
    /// <summary>
    /// When specified, the variable's value will be stored on the specified <see cref="Elsa.Workflows.Core.Services.IStorageDriver"/>. 
    /// </summary>
    public string? StorageDriverId { get; set; }
    
    public override MemoryBlock Declare() => new(Value);
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
        Value = value;
    }

    public T? Get(ActivityExecutionContext context) => Get(context.ExpressionExecutionContext).ConvertTo<T?>();
    public new T? Get(ExpressionExecutionContext context) => base.Get(context).ConvertTo<T?>();
}