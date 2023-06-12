using System.Linq.Expressions;

namespace Elsa.Common.Entities;

/// <summary>
/// Represents the order by which to order the results of a query.
/// </summary>
public class OrderDefinition<T, TProp>
{
    /// <summary>
    /// Creates a new instance of the <see cref="OrderDefinition{T, TProp}"/> class.
    /// </summary>
    public OrderDefinition()
    {
    }

    /// <summary>
    /// Creates a new instance of the <see cref="OrderDefinition{T, TProp}"/> class.
    /// </summary>
    public OrderDefinition(Expression<Func<T, TProp>> keySelector, OrderDirection direction)
    {
        KeySelector = keySelector;
        Direction = direction;
    }
    
    /// <summary>
    /// The direction in which to order the results.
    /// </summary>
    public OrderDirection Direction { get; set; }
    
    /// <summary>
    /// The key selector to use to order the results.
    /// </summary>
    public Expression<Func<T, TProp>> KeySelector { get; set; } = default!;
}