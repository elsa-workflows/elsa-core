using System;
using System.Collections.Generic;
using Elsa.Client.Models;
using Newtonsoft.Json.Linq;
using ProtoBuf.Meta;

namespace ElsaDashboard.Shared.Surrogates
{
    /// <summary>
    /// Provides an extension method to conveniently register surrogate types with the default <see cref="RuntimeTypeModel"/>.
    /// </summary>
    public static class RuntimeTypeModelExtensions
    {
        private static readonly IDictionary<Type, Type> SurrogateMapping = new Dictionary<Type, Type>
        {
            [typeof(ActivityPropertyDescriptor)] = typeof(ActivityPropertyDescriptorSurrogate)
        };

        /// <summary>
        /// Register all NodaTime surrogate types with the protobuf runtime model.
        /// </summary>
        public static RuntimeTypeModel AddElsaGrpcSurrogates(this RuntimeTypeModel runtimeTypeModel)
        {
            foreach (var map in SurrogateMapping) 
                runtimeTypeModel.Add(map.Key, false).SetSurrogate(map.Value);
            
            return runtimeTypeModel;
        }
    }
}