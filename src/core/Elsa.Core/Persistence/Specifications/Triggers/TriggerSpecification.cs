using System;
using System.Linq.Expressions;
using Elsa.Models;

namespace Elsa.Persistence.Specifications.Triggers;

public class TriggerSpecification : Specification<Trigger>
{
    public TriggerSpecification(string activityType, string? tenantId)
    {
        ActivityType = activityType;
        TenantId = tenantId;
    }

    public string? TenantId { get; set; }
    public string ActivityType { get; set; }

    public override Expression<Func<Trigger, bool>> ToExpression() => trigger => trigger.TenantId == TenantId
                                                                                 && trigger.ActivityType == ActivityType;
}