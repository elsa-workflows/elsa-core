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

        public ISetupActivity<T> Set<TProperty>(Expression<Func<T, TProperty?>> propertyAccessor, Func<ActivityExecutionContext, ValueTask<TProperty?>> valueFactory)
        {
            var propertyInfo = propertyAccessor.GetProperty()!;
            ValueProviders[propertyInfo.Name] = async context => await valueFactory(context);
            return this;
        }
        
        public ISetupActivity<T> WithStorageFor<TProperty>(Expression<Func<T, TProperty?>> propertyAccessor, string? storageProviderName)
        {
            var propertyInfo = propertyAccessor.GetProperty()!;
            
            if(storageProviderName != null)
                StorageProviders[propertyInfo.Name] = storageProviderName;
            else
                StorageProviders.Remove(propertyInfo.Name);
            
            return this;
        }
    }
}