using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Elsa.Models;

namespace Elsa.Persistence.Specifications.Triggers;

public class TriggerIdsSpecification : Specification<Trigger>
{
    public TriggerIdsSpecification(IEnumerable<string> ids) => Ids = ids;
    public IEnumerable<string> Ids { get; }
    public override Expression<Func<Trigger, bool>> ToExpression() => trigger => Ids.Contains(trigger.Id);
}