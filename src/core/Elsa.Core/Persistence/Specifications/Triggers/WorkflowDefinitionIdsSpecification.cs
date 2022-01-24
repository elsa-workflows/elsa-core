using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Elsa.Models;

namespace Elsa.Persistence.Specifications.Triggers;

public class WorkflowDefinitionIdsSpecification : Specification<Trigger>
{
    public WorkflowDefinitionIdsSpecification(IEnumerable<string> workflowDefinitionIds)
    {
        WorkflowDefinitionIds = workflowDefinitionIds;
    }

    public IEnumerable<string> WorkflowDefinitionIds { get; }

    public override Expression<Func<Trigger, bool>> ToExpression() => trigger => WorkflowDefinitionIds.Contains(trigger.WorkflowDefinitionId);
}