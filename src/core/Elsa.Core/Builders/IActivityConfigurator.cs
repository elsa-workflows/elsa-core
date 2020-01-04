using System;
using System.Linq.Expressions;
using Elsa.Services.Models;

namespace Elsa.Builders
{
    public interface IActivityConfigurator<T> where T : class, IActivity
    {
        IActivityConfigurator<T> WithId(string value);
        IActivityConfigurator<T> WithName(string name);
        IActivityConfigurator<T> WithDisplayName(string displayName);
        IActivityConfigurator<T> WithDescription(string description);
        IActivityConfigurator<T> With<TProperty>(Expression<Func<T, TProperty>> propertyExpression, Func<TProperty> propertyValueProvider);
        T Build();
    }
}