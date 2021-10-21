using System;
using System.Linq.Expressions;
using Elsa.Models;
using NodaTime;

namespace Elsa.Persistence.Specifications.WorkflowInstances
{
    public class WorkflowCreatedBeforeSpecification : Specification<WorkflowInstance>
    {
        public WorkflowCreatedBeforeSpecification(Instant instant) => Instant = instant;
        public Instant Instant { get; }

        public override Expression<Func<WorkflowInstance, bool>> ToExpression() => x => x.CreatedAt <= Instant;
    }
}