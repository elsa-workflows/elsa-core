using Elsa.Workflows.Models;

namespace Elsa.Scripting.Dsl.Contracts;

public interface ITypeSystem
{
    void Register(TypeDescriptor descriptor);
    TypeDescriptor? ResolveTypeName(string typeName);
}