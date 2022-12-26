using Elsa.Expressions.Helpers;
using Elsa.Expressions.Models;
using Elsa.Workflows.Core.Services;

namespace Elsa.Workflows.Core.Models;

public class Variable : MemoryBlockReference
{
    public Variable()
    {
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
    /// The ID of a storage driver to use for persistence.
    /// If not driver is specified, the referenced memory block will remain in memory for as long as the expression execution context exists.
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