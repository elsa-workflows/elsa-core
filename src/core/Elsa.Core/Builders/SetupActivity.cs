using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa.Builders
{
    public class SetupActivity<T> : ISetupActivity<T> where T : IActivity
    {
        public IDictionary<string, Func<ActivityExecutionContext, ValueTask<object?>>> ValueProviders { get; } = new Dictionary<string, Func<ActivityExecutionContext, ValueTask<object?>>>();
        public IDictionary<string, string> StorageProviders { get; } = new Dictionary<string, string>();


        public ISetupActivity<T> Set<TProperty>(string propertyName, Func<ActivityExecutionContext, ValueTask<TProperty?>> valueFactory)
        {
            ValueProviders[propertyName] = async context => await valueFactory(context);
            return this;
        }

        public ISetupActivity<T> WithStorageFor<TProperty>(string propertyName, string? storageProviderName)
        {
            if (storageProviderName != null)
                StorageProviders[propertyName] = storageProviderName;
            else
                StorageProviders.Remove(propertyName);

            return this;
        }

        public ISetupActivity<T> Set<TProperty>(Expression<Func<T, TProperty?>> propertyAccessor, Func<ActivityExecutionContext, ValueTask<TProperty?>> valueFactory) =>
            Set(propertyAccessor.GetProperty()!.Name, valueFactory);
        
        public ISetupActivity<T> WithStorageFor<TProperty>(Expression<Func<T, TProperty?>> propertyAccessor, string? storageProviderName) =>
            WithStorageFor<T>(propertyAccessor.GetProperty()!.Name, storageProviderName);
    }
}