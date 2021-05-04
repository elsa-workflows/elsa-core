using System;
using System.Linq.Expressions;
using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa.Builders
{
    public static class ActivityFluentSetup
    {
        public static T WithId<T>(this T activity, string value) where T : class, IActivity =>
            With(activity, x => x.Id = value);

        public static T WithName<T>(this T activity, string value) where T : class, IActivity =>
            With(activity, x => x.Name = value);

        public static T WithDisplayName<T>(this T activity, string value) where T : class, IActivity =>
            With(activity, x => x.DisplayName = value);

        public static T WithDescription<T>(this T activity, string value) where T : class, IActivity =>
            With(activity, x => x.Description = value);

        public static T With<T>(this T activity, Action<T> setup) where T : class, IActivity
        {
            setup(activity);
            return activity;
        }

        public static T With<T, TProperty>(this T activity,
            Expression<Func<T, TProperty>> propertyAccessor,
            TProperty value) where T : class, IActivity =>
            activity.With(a => a.SetPropertyValue(propertyAccessor, value));

        public static T With<T, TProperty>(this T activity,
            Expression<Func<T, TProperty>> propertyAccessor,
            Func<TProperty>? value) where T : class, IActivity =>
            value == null ? activity : activity.With(a => a.SetPropertyValue(propertyAccessor, value()));

        public static T With<T, TProperty>(this T activity,
            Expression<Func<T, TProperty>> propertyAccessor,
            Func<ActivityExecutionContext, TProperty>? value) where T : class, IActivity =>
            value == null ? activity : activity.With(a => a.SetPropertyValue(propertyAccessor, value(null!)));
    }
}