using System;

namespace Elsa.Scripting.JavaScript.Services
{
    public interface ITypeDefinitionProvider
    {
        bool SupportsType(Type type);
        string GetTypeDefinition(Type type);
    }
}