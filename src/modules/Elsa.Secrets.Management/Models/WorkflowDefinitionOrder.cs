using System.Linq.Expressions;
using Elsa.Common.Entities;
using Elsa.Secrets.Management.Entities;
using JetBrains.Annotations;

namespace Elsa.Secrets.Management;

/// Represents the order by which to order the results of a query.
[PublicAPI]
public class SecretOrder<TProp> : OrderDefinition<Secret, TProp>
{
    /// <inheritdoc />
    public SecretOrder()
    {
    }
    
    /// Creates a new instance of the <see cref="SecretOrder{TProp}"/> class.
    public SecretOrder(Expression<Func<Secret, TProp>> keySelector, OrderDirection direction) : base(keySelector, direction)
    {
    }
}