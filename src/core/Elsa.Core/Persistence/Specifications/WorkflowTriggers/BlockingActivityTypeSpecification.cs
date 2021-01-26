using System;
using System.Linq;
using System.Linq.Expressions;
using Elsa.Models;

namespace Elsa.Persistence.Specifications.WorkflowTriggers
{
    public class TriggerSpecification : Specification<WorkflowTrigger>
    {
        public TriggerSpecification(string hash, string activityType, string? tenantId)
        {
            Hash = hash;
            ActivityType = activityType;
            TenantId = tenantId;
        }

        public string? TenantId { get; set; }
        public string Hash { get; }
        public string ActivityType { get; set; }
        
        public override Expression<Func<WorkflowTrigger, bool>> ToExpression() => instance => 
            instance.TenantId == TenantId &&
            instance.ActivityType == ActivityType &&
            instance.Hash == Hash;
    }
}