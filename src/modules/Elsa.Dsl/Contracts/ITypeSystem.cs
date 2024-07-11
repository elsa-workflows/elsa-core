using Elsa.Workflows.Models;

namespace Elsa.Dsl.Contracts;

public interface ITypeSystem
{
    void Register(TypeDescriptor descriptor);
    TypeDescriptor? ResolveTypeName(string typeName);
}