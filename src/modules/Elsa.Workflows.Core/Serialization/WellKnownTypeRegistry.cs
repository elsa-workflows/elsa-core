using System;
using System.Collections.Generic;
using Elsa.Workflows.Core.Services;

namespace Elsa.Workflows.Core.Serialization;

public class WellKnownTypeRegistry : IWellKnownTypeRegistry
{
    private readonly IDictionary<string, Type> _aliasTypeDictionary = new Dictionary<string, Type>();
    private readonly IDictionary<Type, string> _typeAliasDictionary = new Dictionary<Type, string>();

    public WellKnownTypeRegistry()
    {
        this.RegisterType<object>("Object");
        this.RegisterType<string>("String");
        this.RegisterType<bool>("Boolean");
        this.RegisterType<int>("Int32");
        this.RegisterType<long>("Int64");
        this.RegisterType<decimal>("Decimal");
        this.RegisterType<float>("Single");
        this.RegisterType<double>("Double");
        this.RegisterType<DateTime>("DateTime");
        this.RegisterType<DateTimeOffset>("DateTimeOffset");
        this.RegisterType<TimeSpan>("TimeSpan");
    }
        
    public void RegisterType(Type type, string alias)
    {
        _typeAliasDictionary[type] = alias;
        _aliasTypeDictionary[alias] = type;
    }

    public bool TryGetAlias(Type type, out string alias) => _typeAliasDictionary.TryGetValue(type, out alias!);
    public bool TryGetType(string alias, out Type type) => _aliasTypeDictionary.TryGetValue(alias, out type!);
}