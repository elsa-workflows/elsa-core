using System;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa.Builders
{
    public interface ISetupActivity
    {
    }

    public interface ISetupActivity<T> : ISetupActivity where T : IActivity
    {
        ISetupActivity<T> Set<TProperty>(Expression<Func<T, TProperty?>> propertyAccessor, Func<ActivityExecutionContext, ValueTask<TProperty?>> valueFactory);
        ISetupActivity<T> WithStorageFor<TProperty>(Expression<Func<T, TProperty?>> propertyAccessor, string? storageProviderName);
    }

    public static class SetupActivityExtensions
    {
        public static ISetupActivity<T> Set<T, TProperty>(
            this ISetupActivity<T> builder,
            Expression<Func<T, TProperty?>> propertyAccessor,
            Func<ActivityExecutionContext, ValueTask<TProperty?>> valueFactory) where T : IActivity =>
            builder.Set(propertyAccessor, async context => await valueFactory(context));
        
        public static ISetupActivity<T> Set<T, TProperty>(
            this ISetupActivity<T> builder,
            Expression<Func<T, TProperty?>> propertyAccessor,
            Func<ActivityExecutionContext, TProperty?> valueFactory) where T : IActivity =>
            builder.Set(propertyAccessor, context => new ValueTask<TProperty?>(valueFactory(context)));
        
        public static ISetupActivity<T> Set<T, TProperty>(
            this ISetupActivity<T> builder,
            Expression<Func<T, TProperty?>> propertyAccessor,
            Func<TProperty?> valueFactory) where T : IActivity =>
            builder.Set(propertyAccessor, context => new ValueTask<TProperty?>(valueFactory()));
        
        public static ISetupActivity<T> Set<T, TProperty>(
            this ISetupActivity<T> builder,
            Expression<Func<T, TProperty?>> propertyAccessor,
            Func<ValueTask<TProperty?>> valueFactory) where T : IActivity =>
            builder.Set(propertyAccessor, async _ => await valueFactory());
        
        public static ISetupActivity<T> Set<T, TProperty>(
            this ISetupActivity<T> builder,
            Expression<Func<T, TProperty?>> propertyAccessor,
            TProperty? value) where T : IActivity =>
            builder.Set(propertyAccessor, _ => new ValueTask<TProperty?>(value));
    }
}