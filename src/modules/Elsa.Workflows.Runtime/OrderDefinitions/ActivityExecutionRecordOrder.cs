using System.Linq.Expressions;
using Elsa.Common.Entities;
using Elsa.Workflows.Runtime.Entities;

namespace Elsa.Workflows.Runtime.OrderDefinitions;

/// <summary>
/// Represents the order by which to order the results of a query.
/// </summary>
public class ActivityExecutionRecordOrder<TProp> : OrderDefinition<ActivityExecutionRecord, TProp>
{
    /// <summary>
    /// Creates a new instance of the <see cref="ActivityExecutionRecordOrder{TProp}"/> class.
    /// </summary>
    public ActivityExecutionRecordOrder(Expression<Func<ActivityExecutionRecord, TProp>> keySelector, OrderDirection direction)
    {
        KeySelector = keySelector;
        Direction = direction;
    }
}