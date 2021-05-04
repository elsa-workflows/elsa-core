using System;
using System.Collections.Generic;
using Elsa.Scripting.JavaScript.Services;

namespace Elsa.Scripting.JavaScript.Typings
{
    public class PrimitiveTypeDefinitionProvider : ITypeDefinitionProvider
    {
        private static readonly IDictionary<Type, string> TypeMap = new Dictionary<Type, string>
        {
            [typeof(short)] = "number",
            [typeof(ushort)] = "number",
            [typeof(int)] = "number",
            [typeof(uint)] = "number",
            [typeof(long)] = "number",
            [typeof(ulong)] = "number",
            [typeof(float)] = "number",
            [typeof(double)] = "number",
            [typeof(decimal)] = "number",
            [typeof(string)] = "string",
            [typeof(char)] = "string",
            [typeof(bool)] = "boolean",
            [typeof(DateTime)] = "Date",
            [typeof(DateTimeOffset)] = "Date",
            [typeof(TimeSpan)] = "string",
        };

        public bool SupportsType(Type type) => TypeMap.ContainsKey(type);
        public string GetTypeDefinition(Type type) => TypeMap[type];
    }
}