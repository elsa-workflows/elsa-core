using System.Linq.Expressions;
using System.Text.Json.Serialization;

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
    [JsonIgnore]
    public Expression<Func<T, TProp>> KeySelector { get; set; } = null!;

    /// <summary>
    /// A stable representation of the key selector for cache-key generation.
    /// </summary>
    public string? KeySelectorText => KeySelector?.ToString();
}
