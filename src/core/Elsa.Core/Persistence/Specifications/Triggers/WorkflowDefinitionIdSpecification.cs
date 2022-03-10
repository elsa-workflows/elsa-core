using System;
using System.Linq.Expressions;
using Elsa.Models;

namespace Elsa.Persistence.Specifications.Triggers;

public class WorkflowDefinitionIdSpecification : Specification<Trigger>
{
    public WorkflowDefinitionIdSpecification(string workflowInstanceId) => WorkflowDefinitionId = workflowInstanceId;
    public string WorkflowDefinitionId { get; }
    public override Expression<Func<Trigger, bool>> ToExpression() => trigger => trigger.WorkflowDefinitionId == WorkflowDefinitionId;
}