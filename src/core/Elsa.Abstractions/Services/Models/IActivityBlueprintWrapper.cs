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
        ValueTask<object?> GetPropertyValueAsync(string propertyName, CancellationToken cancellationToken = default);
    }

    public interface IActivityBlueprintWrapper<TActivity> : IActivityBlueprintWrapper where TActivity:IActivity
    {
        ValueTask<T?> GetPropertyValueAsync<T>(Expression<Func<TActivity, T>> propertyExpression, CancellationToken cancellationToken = default);
        T? GetState<T>(Expression<Func<TActivity, T>> propertyExpression);
    }
}