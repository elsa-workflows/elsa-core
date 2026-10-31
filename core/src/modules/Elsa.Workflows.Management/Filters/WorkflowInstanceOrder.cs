using System.Linq.Expressions;
using Elsa.Common.Entities;
using Elsa.Workflows.Management.Entities;

namespace Elsa.Workflows.Management.Filters;

/// <summary>
/// Represents the order by which to order the results of a query.
/// </summary>
public class WorkflowInstanceOrder<TProp> : OrderDefinition<WorkflowInstance, TProp>
{
    /// <inheritdoc />
    public WorkflowInstanceOrder()
    {
    }

    /// <summary>
    /// Creates a new instance of the <see cref="WorkflowInstanceOrder{TProp}"/> class.
    /// </summary>
    public WorkflowInstanceOrder(Expression<Func<WorkflowInstance, TProp>> keySelector, OrderDirection direction) : base(keySelector, direction)
    {
    }
}