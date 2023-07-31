using Elsa.Expressions.Contracts;
using Elsa.Expressions.Models;

namespace Elsa.Expressions;

/// <summary>
/// An expression that is backed by a delegate.
/// </summary>
public class DelegateExpression : IExpression
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DelegateExpression"/> class.
    /// </summary>
    /// <param name="delegateBlockReference">The delegate block reference.</param>
    public DelegateExpression(DelegateBlockReference delegateBlockReference)
    {
        DelegateBlockReference = delegateBlockReference;
    }
        
    /// <summary>
    /// Gets the delegate block reference.
    /// </summary>
    public DelegateBlockReference DelegateBlockReference { get; }
}

/// <summary>
/// An expression that is backed by a delegate.
/// </summary>
/// <typeparam name="T">The value type.</typeparam>
public class DelegateExpression<T> : DelegateExpression
{
    /// <inheritdoc />
    public DelegateExpression(DelegateBlockReference<T> delegateBlockReference) : base(delegateBlockReference)
    {
    }

    /// <inheritdoc />
    public DelegateExpression(Func<T?> @delegate) : this(new DelegateBlockReference<T>(@delegate))
    {
    }

    /// <inheritdoc />
    public DelegateExpression(Func<ExpressionExecutionContext, T?> @delegate) : this(new DelegateBlockReference<T>(@delegate))
    {
    }

    /// <inheritdoc />
    public DelegateExpression(Func<ValueTask<T?>> @delegate) : this(new DelegateBlockReference<T>(_ => @delegate()))
    {
    }

    /// <inheritdoc />
    public DelegateExpression(Func<ExpressionExecutionContext, ValueTask<T?>> @delegate) : this(new DelegateBlockReference<T>(@delegate))
    {
    }
}