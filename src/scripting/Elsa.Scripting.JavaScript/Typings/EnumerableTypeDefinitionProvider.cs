using System;
using System.Collections;
using Elsa.Scripting.JavaScript.Services;

namespace Elsa.Scripting.JavaScript.Typings
{
    public class EnumerableTypeDefinitionProvider : ITypeDefinitionProvider
    {
        public bool SupportsType(Type type) => typeof(IEnumerable).IsAssignableFrom(type);

        public string GetTypeDefinition(Type type)
        {
            return "[]";
        }
    }
}