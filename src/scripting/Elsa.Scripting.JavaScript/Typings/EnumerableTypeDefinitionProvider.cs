using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Elsa.Scripting.JavaScript.Services;
using NodaTime;

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