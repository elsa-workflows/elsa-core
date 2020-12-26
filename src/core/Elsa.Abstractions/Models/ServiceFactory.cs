using System;
using System.Collections.Generic;
using System.Linq;

namespace Elsa.Models
{
    public class ServiceFactory<TService>
    {
        private readonly IDictionary<Type, Func<IServiceProvider, TService>> _dictionary = new Dictionary<Type, Func<IServiceProvider, TService>>();

        public IReadOnlyCollection<Type> Types => _dictionary.Keys.ToList().AsReadOnly();
        public void Add(Type type, Func<IServiceProvider, TService> factory) => _dictionary[type] = factory;
        public void Add(Type type, Func<TService> factory) => _dictionary[type] = _ => factory();
        public void Add(Type type, TService instance) => _dictionary[type] = _ => instance;

        public void Remove(Type type)
        {
            if (_dictionary.ContainsKey(type))
                _dictionary.Remove(type);
        }
        
        public TService CreateService(Type type, IServiceProvider scope) => _dictionary[type].Invoke(scope);
        public IEnumerable<TService> CreateServices(IServiceProvider scope) => _dictionary.Values.Select(factory => factory(scope));
    }
}
