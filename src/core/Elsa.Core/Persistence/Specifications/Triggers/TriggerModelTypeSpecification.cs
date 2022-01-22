using System;
using System.Linq.Expressions;
using Elsa.Models;
using Elsa.Services;
using Rebus.Extensions;

namespace Elsa.Persistence.Specifications.Triggers;

public class TriggerModelTypeSpecification : Specification<Trigger>
{
    public TriggerModelTypeSpecification(string modelType) => ModelType = modelType;

    public string ModelType { get; }

    public override Expression<Func<Trigger, bool>> ToExpression() => trigger => ModelType.Equals(trigger.ModelType);

    public static TriggerModelTypeSpecification For<T>() where T : IBookmark => new(typeof(T).GetSimpleAssemblyQualifiedName());
}