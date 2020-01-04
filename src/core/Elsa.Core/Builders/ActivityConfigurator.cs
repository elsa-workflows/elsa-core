using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Elsa.Extensions;
using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa.Builders
{
    public class ActivityConfigurator<T> : IActivityConfigurator<T> where T : class, IActivity
    {
        public ActivityConfigurator(T activity)
        {
            Activity = activity;
        }

        public ActivityConfigurator(IActivityResolver activityResolver)
        {
            ActivityResolver = activityResolver;
            Activity = activityResolver.ResolveActivity<T>();
        }
     
        protected IActivityResolver ActivityResolver { get; }
        public T Activity { get; }
        
        public IActivityConfigurator<T> With<TProperty>(Expression<Func<T, TProperty>> propertyExpression, Func<TProperty> propertyValueProvider)
        {
            var value = propertyValueProvider();
            Activity.SetPropertyValue(propertyExpression, value);
            return this;
        }

        public IActivityConfigurator<T> WithId(string value)
        {
            Activity.Id = value;
            return this;
        }

        public IActivityConfigurator<T> WithName(string name)
        {
            Activity.Name = name;
            return this;
        }

        public IActivityConfigurator<T> WithDisplayName(string displayName)
        {
            Activity.DisplayName = displayName;
            return this;
        }

        public IActivityConfigurator<T> WithDescription(string description)
        {
            Activity.Description = description;
            return this;
        }
        
        public virtual T Build() => Activity;
    }
}