using System;
using System.Collections.Generic;
using System.Globalization;
using Elsa.Models;
using Elsa.Scripting.JavaScript.Services;
using Elsa.Services.Models;
using Microsoft.AspNetCore.Http;
using NodaTime;

namespace Elsa.Samples.Server.Host
{
    public class ClientTypeDefinitionProvider : TypeDefinitionProvider
    {
        private static readonly IDictionary<Type, string> TypeMap = new Dictionary<Type, string>
        {
            [typeof(Client)] = "Client"
        };

        public override bool SupportsType(TypeDefinitionContext context, Type type) => TypeMap.ContainsKey(type);
        public override bool ShouldRenderType(TypeDefinitionContext context, Type type) => true;
        public override string GetTypeDefinition(TypeDefinitionContext context, Type type) => TypeMap[type];

        public override IEnumerable<Type> CollectTypes(TypeDefinitionContext context) => new[]
        {
            typeof(Client)
        };
    }
}
