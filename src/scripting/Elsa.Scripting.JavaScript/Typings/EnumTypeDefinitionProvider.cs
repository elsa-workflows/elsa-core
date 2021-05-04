using System;
using Elsa.Scripting.JavaScript.Services;

namespace Elsa.Scripting.JavaScript.Typings
{
    public class EnumTypeDefinitionProvider : ITypeDefinitionProvider
    {
        public bool SupportsType(Type type) => type.IsEnum;
        public string GetTypeDefinition(Type type) => "number";
    }
}