using System.Linq.Expressions;
using Elsa.Common.Entities;
using Elsa.Workflows.Management.Entities;
using JetBrains.Annotations;

namespace Elsa.Workflows.Management.Filters;

/// <summary>
/// Represents the order by which to order the results of a query.
/// </summary>
[PublicAPI]
public class WorkflowDefinitionOrder<TProp> : OrderDefinition<WorkflowDefinition, TProp>
{
    /// <inheritdoc />
    public WorkflowDefinitionOrder()
    {
    }

    /// <summary>
    /// Creates a new instance of the <see cref="WorkflowDefinitionOrder{TProp}"/> class.
    /// </summary>
    public WorkflowDefinitionOrder(Expression<Func<WorkflowDefinition, TProp>> keySelector, OrderDirection direction) : base(keySelector, direction)
    {
    }
}