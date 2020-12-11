﻿using System;
using System.Collections.Generic;
using Elsa.Client.Models;
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
            [typeof(ActivityPropertyInfo)] = typeof(ActivityPropertyDescriptorSurrogate),
            [typeof(VersionOptions)] = typeof(VersionOptionsSurrogate),
            [typeof(WorkflowInstance)] = typeof(WorkflowInstanceSurrogate),
            [typeof(WorkflowDefinition)] = typeof(WorkflowDefinitionSurrogate),
            [typeof(WorkflowBlueprint)] = typeof(WorkflowBlueprintSurrogate)
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