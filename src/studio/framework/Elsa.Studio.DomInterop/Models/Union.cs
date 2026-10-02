namespace Elsa.Studio.Models;

/// <summary>
/// Represents a discriminated union that can hold a value of either <typeparamref name="T1"/> or <typeparamref name="T2"/>.
/// </summary>
/// <typeparam name="T1">The first supported value type.</typeparam>
/// <typeparam name="T2">The second supported value type.</typeparam>
public class Union<T1, T2>
{
    private readonly T1? _value1;
    private readonly T2? _value2;
    private readonly bool _isValue1;

    /// <summary>
    /// Initializes a new <see cref="Union{T1, T2}"/> instance that stores a value of type <typeparamref name="T1"/>.
    /// </summary>
    /// <param name="value">The value to store.</param>
    protected Union(T1 value)
    {
        _value1 = value;
        _isValue1 = true;
    }

    /// <summary>
    /// Initializes a new <see cref="Union{T1, T2}"/> instance that stores a value of type <typeparamref name="T2"/>.
    /// </summary>
    /// <param name="value">The value to store.</param>
    protected Union(T2 value)
    {
        _value2 = value;
        _isValue1 = false;
    }

    /// <summary>
    /// Implicitly converts a <typeparamref name="T1"/> value into a <see cref="Union{T1, T2}"/>.
    /// </summary>
    /// <param name="value">The value to wrap.</param>
    /// <returns>A union containing <paramref name="value"/>.</returns>
    public static implicit operator Union<T1, T2>(T1 value) => new(value);

    /// <summary>
    /// Implicitly converts a <typeparamref name="T2"/> value into a <see cref="Union{T1, T2}"/>.
    /// </summary>
    /// <param name="value">The value to wrap.</param>
    /// <returns>A union containing <paramref name="value"/>.</returns>
    public static implicit operator Union<T1, T2>(T2 value) => new(value);

    /// <summary>
    /// Executes the matching function corresponding to the stored value type and returns its result.
    /// </summary>
    /// <typeparam name="T">The type of result produced by the matching functions.</typeparam>
    /// <param name="case1">The function to invoke when the union contains a <typeparamref name="T1"/> value.</param>
    /// <param name="case2">The function to invoke when the union contains a <typeparamref name="T2"/> value.</param>
    /// <returns>The result of the executed matching function.</returns>
    public T Match<T>(Func<T1, T> case1, Func<T2, T> case2)
    {
        if (case1 == null) throw new ArgumentNullException(nameof(case1));
        if (case2 == null) throw new ArgumentNullException(nameof(case2));

        return _isValue1 ? case1(_value1!) : case2(_value2!);
    }

    /// <summary>
    /// Returns the raw value stored inside the union without performing any conversion.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public object Match() => _isValue1 ? _value1! : _value2!;
}