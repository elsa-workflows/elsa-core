using System;
using System.Collections.Generic;
using System.Globalization;
using Elsa.Scripting.JavaScript.Services;

namespace Elsa.Scripting.JavaScript.Typings
{
    public class BlacklistedTypeDefinitionProvider : TypeDefinitionProvider
    {
        private static readonly IDictionary<Type, string> WhiteList = new Dictionary<Type, string>
        {
            [typeof(Guid)] = "Guid",
        };
        
        private static readonly HashSet<Type> BlackList = new()
        {
            typeof(CompareInfo),
        };

        private static readonly HashSet<string> Namespaces = new()
        {
            "System", 
            "System.Collections", 
            "System.Collections.Generic",
            "System.Reflection", 
            "System.Runtime",
            "System.Runtime.InteropServices",
            "System.Runtime.Serialization",
            "System.Threading",
            "System.Threading.Tasks", 
            "MediatR",
            "Microsoft.Win32.SafeHandles",
            "Newtonsoft.Json", 
            "Newtonsoft.Json.Linq",
            "Newtonsoft.Json.Serialization"
        };

        public override bool SupportsType(TypeDefinitionContext context, Type type) => !WhiteList.ContainsKey(type) && (Namespaces.Contains(type.Namespace) || BlackList.Contains(type));

        public override string GetTypeDefinition(TypeDefinitionContext context, Type type)
        {
            return "any";
        }
    }
}