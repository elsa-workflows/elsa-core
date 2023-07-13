using Elsa.Expressions.Helpers;
using Elsa.Expressions.Models;
using Elsa.Workflows.Core.Contracts;

namespace Elsa.Workflows.Core.Memory;

public class Variable : MemoryBlockReference
{
    public Variable()
    {
        Id = Guid.NewGuid().ToString("N");
    }

    public Variable(string name) : this()
    {
        Name = name;
    }

    public Variable(string name, object? value = default) : this()
    {
        Name = name;
        Value = value;
    }

    public string Name { get; set; }
    
    public object? Value { get; set; }
    
    /// <summary>
    /// The storage driver type to use for persistence.
    /// If no driver is specified, the referenced memory block will remain in memory for as long as the expression execution context exists.
    /// </summary>
    public Type? StorageDriverType { get; set; }

    /// <inheritdoc />
    public override MemoryBlock Declare() => new(Value, new VariableBlockMetadata(this, StorageDriverType, false));
}

public class Variable<T> : Variable
{
    public Variable()
    {
    }

    public Variable(T value)
    {
        Value = value;
    }
    
    public Variable(string name, T value)
    {
        Name = name;
        Value = value;
    }

    public T? Get(ActivityExecutionContext context) => Get(context.ExpressionExecutionContext).ConvertTo<T?>();
    public new T? Get(ExpressionExecutionContext context) => base.Get(context).ConvertTo<T?>();

    /// <summary>
    /// Sets the <see cref="Variable.StorageDriverType"/> to the specified type.
    /// </summary>
    public Variable<T> WithStorageDriver<TDriver>() where TDriver:IStorageDriver
    {
        StorageDriverType = typeof(TDriver);
        return this;
    }
}

/// <summary>
/// Provides metadata about the variable block.
/// </summary>
public record VariableBlockMetadata(Variable Variable, Type? StorageDriverType, bool IsInitialized);