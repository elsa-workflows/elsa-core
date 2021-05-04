using System;
using System.Collections.Generic;
using Rebus.Routing.TypeBased;

namespace Elsa
{
    public static class TypeBaseRouterConfigurationBuilderExtensions
    {
        public static TypeBasedRouterConfigurationExtensions.TypeBasedRouterConfigurationBuilder Map(this TypeBasedRouterConfigurationExtensions.TypeBasedRouterConfigurationBuilder builder, IDictionary<Type, string> map)
        {
            foreach (var entry in map) builder.Map(entry.Key, entry.Value);
            return builder;
        }
    }
}