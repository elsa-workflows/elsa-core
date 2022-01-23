using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Elsa.Models;
using Elsa.Services;
using Rebus.Extensions;

namespace Elsa.Persistence.Specifications.Triggers;

public class TriggerSpecification : Specification<Trigger>
{
    public TriggerSpecification(string activityType, IEnumerable<string> hashes, string? tenantId)
    {
        ActivityType = activityType;
        Hashes = hashes;
        TenantId = tenantId;
    }

    public string? TenantId { get; }
    public string ActivityType { get; }
    public IEnumerable<string> Hashes { get; }

    public override Expression<Func<Trigger, bool>> ToExpression() => trigger => trigger.TenantId == TenantId && trigger.ActivityType == ActivityType && Hashes.Contains(trigger.Hash);
    public static TriggerSpecification For<T>(IEnumerable<string> hashes, string? tenantId = default) where T : IActivity => new(typeof(T).GetSimpleAssemblyQualifiedName(), hashes, tenantId);
}