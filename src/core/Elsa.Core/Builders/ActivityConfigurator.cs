using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Elsa.Services.Models;

namespace Elsa.Builders
{
    public class ActivityConfigurator<T> : IActivityConfigurator<T> where T : class, IActivity
    {
        public ActivityConfigurator(T activity)
        {
            Activity = activity;
        }
        
        public T Activity { get; }
        
        public IActivityConfigurator<T> WithProperty<TProperty>(Expression<Func<T, TProperty>> propertyExpression, Func<TProperty> propertyValueProvider)
        {
            return this;
        }

        public void WithId(string value)
        {
            Activity.Id = value;
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
        
        public IActivity Build() => Activity;
    }
}