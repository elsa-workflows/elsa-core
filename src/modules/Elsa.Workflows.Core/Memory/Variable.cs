using Elsa.Expressions.Helpers;
using Elsa.Expressions.Models;
using Humanizer;

namespace Elsa.Workflows.Memory;

/// <summary>
/// Represents a variable that references a memory block.
/// </summary>
public class Variable : MemoryBlockReference
{
    /// <inheritdoc />
    public Variable()
    {
    }

    /// <inheritdoc />
    public Variable(string name)
    {
        Id = GetIdFromName(name);
        Name = name;
    }

    /// <inheritdoc />
    public Variable(string name, object? value = null) : this(name)
    {
        Value = value;
    }
    
    public Variable(string name, object? value = null, string? id = null) : this(name, value)
    {
        Value = value;
    }

    /// <summary>
    /// The name of the variable.
    /// </summary>
    public string Name { get; set; } = null!;
    
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

    private string GetIdFromName(string? name) => $"{name?.Camelize() ?? "Unnamed"}{nameof(Variable)}";
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
    [Obsolete("Use the constructor that takes a name parameter instead.", true)]
    public Variable(T value)
    {
        Value = value;
    }

    /// <inheritdoc />
    public Variable(string name, T value) : base(name, value)
    {
    }
    
    public Variable(string name, T value, string? id = null) : base(name, value, id)
    {
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

    public Variable<T> WithId(string id)
    {
        Id = id;
        return this;
    }
    
    public Variable<T> WithName(string name)
    {
        Name = name;
        return this;
    }
    
    public Variable<T> WithValue(T value)
    {
        Value = value;
        return this;
    }
}

/// <summary>
/// Provides metadata about the variable block.
/// </summary>
public record VariableBlockMetadata(Variable Variable, Type? StorageDriverType, bool IsInitialized);