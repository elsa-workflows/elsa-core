using System;
using System.Linq.Expressions;
using Elsa.Models;

namespace Elsa.Persistence.Specifications.Bookmarks
{
    public class WorkflowInstanceIdSpecification : Specification<Bookmark>
    {
        public WorkflowInstanceIdSpecification(string workflowInstanceId) => WorkflowInstanceId = workflowInstanceId;
        public string WorkflowInstanceId { get; }
        public override Expression<Func<Bookmark, bool>> ToExpression() => trigger => trigger.WorkflowInstanceId == WorkflowInstanceId;
    }
}