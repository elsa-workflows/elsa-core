using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Elsa.Models;

namespace Elsa.Persistence.Specifications.Bookmarks
{
    public class WorkflowInstanceIdsSpecification : Specification<Bookmark>
    {
        public WorkflowInstanceIdsSpecification(IEnumerable<string> workflowInstanceIds)
        {
            WorkflowInstanceIds = workflowInstanceIds;
        }

        public IEnumerable<string> WorkflowInstanceIds { get; }

        public override Expression<Func<Bookmark, bool>> ToExpression() => trigger => WorkflowInstanceIds.Contains(trigger.WorkflowInstanceId);
    }
}