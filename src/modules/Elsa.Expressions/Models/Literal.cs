namespace Elsa.Expressions.Models;

/// <summary>
/// A literal expression that represents a constant value.
/// </summary>
public class Literal : MemoryBlockReference
{
    /// <inheritdoc />
    public Literal()
    {
    }

    /// <inheritdoc />
    public Literal(object? value, string? id = default) : base(id!)
    {
        Value = value;
    }
        
    /// <summary>
    /// Gets the value of the literal.
    /// </summary>
    public object? Value { get; }

    /// <inheritdoc />
    public override MemoryBlock Declare() => new();

    /// <summary>
    /// Creates a literal expression from a value.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <typeparam name="T">The value type.</typeparam>
    /// <returns>A literal expression.</returns>
    public static Literal From<T>(T value) => new Literal<T>(value);
}

/// <summary>
/// A literal expression that represents a constant value.
/// </summary>
/// <typeparam name="T">The value type.</typeparam>
public class Literal<T> : Literal
{
    /// <inheritdoc />
    public Literal()
    {
    }

    /// <inheritdoc />
    public Literal(T value, string? id = default) : base(value!, id)
    {
    }
}