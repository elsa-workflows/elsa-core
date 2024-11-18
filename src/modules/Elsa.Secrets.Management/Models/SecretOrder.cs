using System.Linq.Expressions;
using Elsa.Common.Entities;
using JetBrains.Annotations;

namespace Elsa.Secrets.Management;

/// <summary>
/// Represents the order by which to order the results of a query.
/// </summary>
[PublicAPI]
public class SecretOrder<TProp> : OrderDefinition<Secret, TProp>
{
    /// <inheritdoc />
    public SecretOrder()
    {
    }
    
    /// <summary>
    /// Creates a new instance of the <see cref="SecretOrder{TProp}"/> class.
    /// </summary>
    public SecretOrder(Expression<Func<Secret, TProp>> keySelector, OrderDirection direction) : base(keySelector, direction)
    {
    }
}