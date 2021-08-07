using System;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace Elsa.Services.Models
{
    public interface IActivityBlueprintWrapper
    {
        IActivityBlueprint ActivityBlueprint { get; }
        IActivityBlueprintWrapper<TActivity> As<TActivity>() where TActivity : IActivity;
        ValueTask<object?> EvaluatePropertyValueAsync(string propertyName, CancellationToken cancellationToken = default);
    }

    public interface IActivityBlueprintWrapper<TActivity> : IActivityBlueprintWrapper where TActivity:IActivity
    {
        ValueTask<T?> EvaluatePropertyValueAsync<T>(Expression<Func<TActivity, T>> propertyExpression, CancellationToken cancellationToken = default);
        T? GetPropertyValue<T>(Expression<Func<TActivity, T>> propertyExpression);
    }
}