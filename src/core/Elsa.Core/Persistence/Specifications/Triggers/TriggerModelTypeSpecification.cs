using System;
using System.Linq.Expressions;
using Elsa.Models;
using Elsa.Services;
using LinqKit;
using Rebus.Extensions;

namespace Elsa.Persistence.Specifications.Triggers;

public class TriggerModelTypeSpecification : Specification<Trigger>
{
    public TriggerModelTypeSpecification(string modelType, string? tenantId = default)
    {
        ModelType = modelType;
        TenantId = tenantId;
    }

    public string ModelType { get; }
    public string? TenantId { get; }

    public override Expression<Func<Trigger, bool>> ToExpression()
    {
        Expression<Func<Trigger, bool>> expression = x => ModelType.Equals(x.ModelType);

        if (!string.IsNullOrWhiteSpace(TenantId))
            expression = expression.And(x => x.TenantId == TenantId);

        return expression;
    }

    public static TriggerModelTypeSpecification For<T>() where T : IBookmark => new(typeof(T).GetSimpleAssemblyQualifiedName());
}