using Elsa.Expressions.Helpers;
using Elsa.Expressions.Models;
using Elsa.Workflows.Core.Contracts;

namespace Elsa.Workflows.Core.Memory;

/// <summary>
/// Represents a variable that references a memory block.
/// </summary>
public class Variable : MemoryBlockReference
{
    /// <inheritdoc />
    public Variable()
    {
        Id = Guid.NewGuid().ToString("N");
    }

    /// <inheritdoc />
    public Variable(string name) : this()
    {
        Name = name;
    }

    /// <inheritdoc />
    public Variable(string name, object? value = default) : this()
    {
        Name = name;
        Value = value;
    }

    /// <summary>
    /// The name of the variable.
    /// </summary>
    public string Name { get; set; } = default!;
    
    /// <summary>
    /// A default value for the variable.
    /// </summary>
    public object? Value { get; set; }
    
    /// <summary>
    /// The storage driver type to use for persistence.
    /// If no driver is specified, the referenced memory block will remain in memory for as long as the expression execution context exists.
    /// </summary>
    public Type? StorageDriverType { get; set; }

    /// <inheritdoc />
    public override MemoryBlock Declare() => new(Value, new VariableBlockMetadata(this, StorageDriverType, false));
}

/// <summary>
/// Represents a variable that references a memory block.
/// </summary>
/// <typeparam name="T">The type of the variable.</typeparam>
public class Variable<T> : Variable
{
    /// <inheritdoc />
    public Variable()
    {
    }

    /// <inheritdoc />
    public Variable(T value)
    {
        Value = value;
    }

    /// <inheritdoc />
    public Variable(string name, T value)
    {
        Name = name;
        Value = value;
    }

    /// <summary>
    /// Gets the value of the variable.
    /// </summary>
    public T? Get(ActivityExecutionContext context) => Get(context.ExpressionExecutionContext).ConvertTo<T?>();
    
    /// <summary>
    /// Gets the value of the variable.
    /// </summary>
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