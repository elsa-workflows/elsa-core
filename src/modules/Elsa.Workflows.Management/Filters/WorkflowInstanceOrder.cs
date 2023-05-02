using Elsa.Common.Entities;
using Elsa.Workflows.Management.Entities;

namespace Elsa.Workflows.Management.Filters;

/// <summary>
/// Represents the order by which to order the results of a query.
/// </summary>
public class WorkflowInstanceOrder<TProp> : OrderDefinition<WorkflowInstance, TProp>
{
}