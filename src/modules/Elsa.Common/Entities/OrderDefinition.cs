using System.Linq.Expressions;

namespace Elsa.Common.Entities;

public class OrderDefinition<T, TProp>
{
    public OrderDirection Direction { get; set; }
    public Expression<Func<T, TProp>> KeySelector { get; set; }
}